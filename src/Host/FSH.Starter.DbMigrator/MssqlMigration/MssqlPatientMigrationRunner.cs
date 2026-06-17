using System.Globalization;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Reads every non-merged patient from a BackChart MSSQL database and upserts them
/// into the target FSH tenant via Mediator (same validation + encryption path as the API).
///
/// <para>Dry-run: validates every mapped command; no writes.</para>
/// <para>Live run: create when PatientCode absent; skip if already exists (idempotent).</para>
/// <para>Failed rows are never silently lost — they are written to a JSON error file.</para>
/// </summary>
internal sealed class MssqlPatientMigrationRunner(
    IServiceProvider services,
    ILogger logger)
{
    private static readonly JsonSerializerOptions ErrorFileOptions =
        new() { WriteIndented = true };

    public async Task<int> RunAsync(
        string sourceConnectionString,
        string tenantId,
        bool dryRun,
        int batchSize,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var tenant = await ResolveTenantAsync(tenantId).ConfigureAwait(false);

        var errors = new List<MigrationError>();
        int created = 0, skipped = 0, validated = 0;
        int offset = 0;

        await using var conn = new SqlConnection(sourceConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-migration] connected to source: {conn.Database}").ConfigureAwait(false);

        // Open the SQL Server symmetric key for the duration of this session so that
        // DECRYPTBYKEY() calls in the SELECT query return plaintext values.
        await using (var openKey = new SqlCommand(MssqlPatientMapper.OpenKeyStatement, conn))
        {
            await openKey.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        await Console.Out.WriteLineAsync(
            "[mssql-migration] symmetric key opened — decryption active").ConfigureAwait(false);

        try
        {
            while (true)
            {
                var batch = await ReadBatchAsync(conn, offset, batchSize, ct).ConfigureAwait(false);
                if (batch.Count == 0) break;

                foreach (var (cmd, pId) in batch)
                {
                    if (dryRun)
                    {
                        ValidateDryRun(cmd, pId, errors);
                        validated++;
                    }
                    else
                    {
                        var result = await UpsertAsync(tenant, cmd, pId, errors, ct).ConfigureAwait(false);
                        if (result == UpsertResult.Created) created++;
                        else skipped++;
                    }
                }

                offset += batch.Count;
                await Console.Out.WriteLineAsync(string.Create(
                    CultureInfo.InvariantCulture,
                    $"[mssql-migration] processed {offset} rows so far (errors: {errors.Count})"))
                    .ConfigureAwait(false);

                if (batch.Count < batchSize) break;
            }
        }
        finally
        {
            // Always close the symmetric key, even if the read loop throws.
            await using var closeKey = new SqlCommand(MssqlPatientMapper.CloseKeyStatement, conn);
            await closeKey.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"[mssql-migration] DRY-RUN complete — validated {validated} rows, {errors.Count} error(s)"))
                .ConfigureAwait(false);
        }
        else
        {
            await Console.Out.WriteLineAsync(string.Create(
                CultureInfo.InvariantCulture,
                $"[mssql-migration] complete — created {created}, skipped {skipped} (already exist), errors {errors.Count}"))
                .ConfigureAwait(false);
        }

        if (errors.Count > 0)
        {
            await WriteErrorFileAsync(errors, dryRun, ct).ConfigureAwait(false);
        }

        return errors.Count == 0 ? 0 : 1;
    }

    private async Task<AppTenantInfo> ResolveTenantAsync(string tenantId)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Tenant '{tenantId}' not found in the tenant catalog. Run 'apply --seed' first.");
        return tenant;
    }

#pragma warning disable CA2100 // offset and batchSize are int — no user-controlled string input
    private static async Task<List<(CreatePatientCommand Cmd, int PId)>> ReadBatchAsync(
        SqlConnection conn, int offset, int batchSize, CancellationToken ct)
    {
        var results = new List<(CreatePatientCommand, int)>();
        await using var cmd = new SqlCommand(
            MssqlPatientMapper.BuildSelectQuery(offset, batchSize), conn);
        cmd.CommandTimeout = 120;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var pId = reader.GetInt32(reader.GetOrdinal("pID"));
            results.Add((MssqlPatientMapper.Map(reader), pId));
        }
        return results;
    }
#pragma warning restore CA2100

    private static void ValidateDryRun(
        CreatePatientCommand cmd, int pId, List<MigrationError> errors)
    {
        var fieldErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(cmd.FirstName))
            fieldErrors.Add("FirstName is required");
        if (string.IsNullOrWhiteSpace(cmd.LastName))
            fieldErrors.Add("LastName is required");
        if (string.IsNullOrWhiteSpace(cmd.Gender))
            fieldErrors.Add("Gender is required");
        if (cmd.DateOfBirth == default)
            fieldErrors.Add("DateOfBirth is required");
        if (cmd.IsMinor && cmd.GuardianFirstName is null)
            fieldErrors.Add("GuardianFirstName is required when IsMinor=true");

        if (fieldErrors.Count > 0)
        {
            errors.Add(new MigrationError(
                pId, cmd.PatientCode, "ValidationFailed", string.Join("; ", fieldErrors)));
        }
    }

    private async Task<UpsertResult> UpsertAsync(
        AppTenantInfo tenant, CreatePatientCommand cmd, int pId,
        List<MigrationError> errors, CancellationToken ct)
    {
        using var scope = services.CreateScope();

        // Set the Finbuckle tenant context so the Patient DbContext applies the correct
        // per-tenant connection string and row-level TenantId isolation.
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var search = await mediator
                .Send(new SearchPatientsQuery(Search: cmd.PatientCode, PageSize: 1), ct)
                .ConfigureAwait(false);

            if (search.Items.Any(p => string.Equals(
                    p.PatientCode, cmd.PatientCode, StringComparison.OrdinalIgnoreCase)))
            {
                return UpsertResult.Skipped;
            }

            await mediator.Send(cmd, ct).ConfigureAwait(false);
            return UpsertResult.Created;
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new MigrationError(pId, cmd.PatientCode, ex.GetType().Name, ex.Message));
            logger.LogWarning(ex,
                "[mssql-migration] row pID={PId} PatientCode={PatientCode} failed: {Message}",
                pId, cmd.PatientCode, ex.Message);
            return UpsertResult.Skipped;
        }
    }

    private static async Task WriteErrorFileAsync(
        List<MigrationError> errors, bool dryRun, CancellationToken ct)
    {
        var suffix = dryRun ? "dry-run" : "live";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = $"migration-errors-{suffix}-{timestamp}.json";
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(errors, ErrorFileOptions),
            ct).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-migration] {errors.Count} error(s) written to {path}").ConfigureAwait(false);
    }

    private enum UpsertResult { Created, Skipped }

    private sealed record MigrationError(int PId, string PatientCode, string ErrorType, string Message);
}
