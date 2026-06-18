using System.Globalization;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Administration.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Migrates the six BackChart/BronstonChiro reference lookup tables (Races, Ethnicities, Languages,
/// SmokingStatuses, PreferredContactMethods, ReferralTypes) into the Administration module, preserving
/// each row's original integer ID so that migrated patients' lookup foreign keys resolve correctly.
///
/// <para>Run this <b>before</b> <see cref="MssqlPatientMigrationRunner"/>.</para>
/// <para><b>Replace semantics:</b> for each table the existing rows (including the EF <c>HasData</c>
/// placeholder seeds) are deleted and the source rows inserted with explicit IDs; the identity sequence
/// is then advanced past the highest preserved ID. A best-effort table whose
/// source read fails is skipped with its existing rows left intact.</para>
/// <para>Dry-run: reads + counts every source table; writes nothing.</para>
/// </summary>
internal sealed class MssqlLookupMigrationRunner(
    IServiceProvider services,
    ILogger logger)
{
    private static readonly JsonSerializerOptions ErrorFileOptions = new() { WriteIndented = true };

    public async Task<int> RunAsync(
        string sourceConnectionString,
        string tenantId,
        bool dryRun,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var tenant = await ResolveTenantAsync(tenantId).ConfigureAwait(false);
        var errors = new List<LookupMigrationError>();

        await using var conn = new SqlConnection(sourceConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-lookups] connected to source: {conn.Database}").ConfigureAwait(false);

        using var scope = services.CreateScope();
        // Set the Finbuckle tenant context so AdministrationDbContext resolves the same physical database
        // the patient migration writes to (Administration entities are IGlobalEntity — no row filter applies).
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();

        foreach (var map in MssqlLookupMapper.Tables)
        {
            List<MssqlLookupMapper.LookupRow> rows;
            try
            {
                rows = await MssqlLookupMapper.ReadAsync(conn, map, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                var kind = map.BestEffort ? "SourceReadSkipped" : "SourceReadFailed";
                errors.Add(new LookupMigrationError(map.TargetTable, kind, ex.Message));
                await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                    $"[mssql-lookups] {map.TargetTable}: source read failed ({ex.Message}) — {(map.BestEffort ? "skipped, existing rows kept" : "TABLE NOT MIGRATED")}"))
                    .ConfigureAwait(false);
                continue;
            }

            if (rows.Count == 0)
            {
                await Console.Out.WriteLineAsync(
                    $"[mssql-lookups] {map.TargetTable}: source returned 0 rows — skipped").ConfigureAwait(false);
                continue;
            }

            if (dryRun)
            {
                await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                    $"[mssql-lookups] {map.TargetTable}: DRY-RUN would replace with {rows.Count} source row(s) (IDs {rows.Min(r => r.Id)}–{rows.Max(r => r.Id)})"))
                    .ConfigureAwait(false);
                continue;
            }

            try
            {
                await ReplaceTableAsync(dbContext, map, rows, ct).ConfigureAwait(false);
                await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                    $"[mssql-lookups] {map.TargetTable}: replaced with {rows.Count} row(s)")).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Per-table isolation: record the failure and continue with the next table.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                errors.Add(new LookupMigrationError(map.TargetTable, ex.GetType().Name, ex.Message));
                logger.LogWarning(ex, "[mssql-lookups] {Table} failed: {Message}", map.TargetTable, ex.Message);
            }
        }

        await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"[mssql-lookups] {(dryRun ? "DRY-RUN " : string.Empty)}complete — {errors.Count} error(s)")).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            await WriteErrorFileAsync(errors, dryRun, ct).ConfigureAwait(false);
        }

        // BestEffort skips of an unconfirmed source table are expected — they don't fail the run.
        return errors.Any(e => e.ErrorType is not "SourceReadSkipped") ? 1 : 0;
    }

    /// <summary>
    /// Deletes all rows in the target table, inserts the source rows with explicit IDs, then advances the
    /// identity sequence past the highest ID — all in one transaction. The target column is
    /// <c>GENERATED BY DEFAULT AS IDENTITY</c>, so explicit-ID inserts are accepted.
    /// </summary>
    private static async Task ReplaceTableAsync(
        AdministrationDbContext dbContext,
        MssqlLookupMapper.LookupTableMap map,
        List<MssqlLookupMapper.LookupRow> rows,
        CancellationToken ct)
    {
        // Target table name comes from the fixed code-defined Tables list, never user input.
        var table = $"{AdministrationDbContext.Schema}.\"{map.TargetTable}\"";

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        // EF1002: the only interpolated value is `table`, drawn from the fixed code-defined Tables
        // whitelist (never user input) — there is no injection surface. Row values are parameterized below.
#pragma warning disable EF1002
        await dbContext.Database
            .ExecuteSqlRawAsync($"DELETE FROM {table}", ct).ConfigureAwait(false);
#pragma warning restore EF1002

        var insertSql = map.HasSnomedCode
            ? $"INSERT INTO {table} (\"Id\", \"Name\", \"SnomedCode\", \"IsActive\", \"IsDeleted\") VALUES (@id, @name, @snomed, TRUE, FALSE)"
            : $"INSERT INTO {table} (\"Id\", \"Name\", \"IsActive\", \"IsDeleted\") VALUES (@id, @name, TRUE, FALSE)";

        foreach (var row in rows)
        {
            var parameters = new List<NpgsqlParameter>
            {
                new("id", row.Id),
                new("name", row.Name),
            };
            if (map.HasSnomedCode)
            {
                parameters.Add(new NpgsqlParameter("snomed", NpgsqlDbType.Text)
                {
                    Value = (object?)row.SnomedCode ?? DBNull.Value,
                });
            }

            await dbContext.Database
                .ExecuteSqlRawAsync(insertSql, parameters.Cast<object>().ToArray())
                .ConfigureAwait(false);
        }

        // Advance the identity sequence so future admin-created rows don't collide with preserved IDs.
        // EF1002: `table` is a fixed whitelisted identifier (see note above), not user input.
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync(
            $"SELECT setval(pg_get_serial_sequence('{table}', 'Id'), (SELECT MAX(\"Id\") FROM {table}))",
            ct).ConfigureAwait(false);
#pragma warning restore EF1002

        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    private async Task<AppTenantInfo> ResolveTenantAsync(string tenantId)
    {
        using var scope = services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        return await store.GetAsync(tenantId).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Tenant '{tenantId}' not found in the tenant catalog. Run 'apply --seed' first.");
    }

    private static async Task WriteErrorFileAsync(
        List<LookupMigrationError> errors, bool dryRun, CancellationToken ct)
    {
        var suffix = dryRun ? "dry-run" : "live";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = $"lookup-migration-errors-{suffix}-{timestamp}.json";
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(errors, ErrorFileOptions), ct)
            .ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-lookups] {errors.Count} error(s) written to {path}").ConfigureAwait(false);
    }

    private sealed record LookupMigrationError(string Table, string ErrorType, string Message);
}
