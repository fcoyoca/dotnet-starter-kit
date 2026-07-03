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
/// Migrates the legacy RxNorm/SNOMED/dose-unit catalog tables into the Administration module's
/// <c>Drugs</c>, <c>AllergyReactions</c> and <c>MedicationDoseUnits</c> tables.
///
/// <para>Run this <b>before</b> <see cref="MssqlPatientMigrationRunner"/> (patient allergy/medication
/// rows reference these catalog entries).</para>
/// <para><b>Replace semantics</b> for the two small lookups (<c>AllergyReactions</c>,
/// <c>MedicationDoseUnits</c>): existing rows (including the EF <c>HasData</c> placeholder seeds) are
/// deleted and the source rows inserted with explicit IDs; the identity sequence is then advanced past
/// the highest preserved ID — mirrors <see cref="MssqlLookupMigrationRunner"/> exactly.</para>
/// <para><c>Drugs</c> is potentially huge (production RXNCONSO), so it is imported via Npgsql binary
/// COPY instead of row-by-row inserts. The table starts empty (Task 7), so no delete-then-insert
/// replace step is required — the table is cleared first anyway so the verb is safely re-runnable, then
/// streamed rows are inserted, skipping duplicate non-null <c>RxAui</c> values (the target has a unique
/// filtered index on that column) and blank names.</para>
/// <para><c>AllergyReactions</c> is best-effort: <c>SnomedAssociation</c> lives in a different database
/// (<c>Bronston</c>) than <c>RXNCONSO</c>/<c>Snomed</c>/<c>MedicationUnitTypes</c>
/// (<c>BronstonAuthenticatingDB</c>), referenced via a hardcoded three-part cross-database name in
/// <see cref="MssqlDrugCatalogMapper.ReactionsSql"/>. If that sibling database isn't reachable in a
/// given environment (different name, permissions), this table is skipped and its existing seeded rows
/// are left intact instead of failing the whole run.</para>
/// <para>Dry-run: reads + counts every source table; writes nothing.</para>
/// </summary>
internal sealed class MssqlDrugCatalogMigrationRunner(
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
        var errors = new List<DrugCatalogMigrationError>();

        await using var conn = new SqlConnection(sourceConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-drug-catalog] connected to source: {conn.Database}").ConfigureAwait(false);

        using var scope = services.CreateScope();
        // Set the Finbuckle tenant context so AdministrationDbContext resolves the same physical database
        // the patient migration writes to (Administration entities are IGlobalEntity — no row filter applies).
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();

        await MigrateAllergyReactionsAsync(conn, dbContext, dryRun, errors, ct).ConfigureAwait(false);
        await MigrateMedicationDoseUnitsAsync(conn, dbContext, dryRun, errors, ct).ConfigureAwait(false);
        await MigrateDrugsAsync(conn, dbContext, dryRun, errors, ct).ConfigureAwait(false);

        await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"[mssql-drug-catalog] {(dryRun ? "DRY-RUN " : string.Empty)}complete — {errors.Count} error(s)")).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            await WriteErrorFileAsync(errors, dryRun, ct).ConfigureAwait(false);
        }

        // BestEffort skip of the AllergyReactions cross-database join is expected — it doesn't fail the run.
        return errors.Any(e => e.ErrorType is not "SourceReadSkipped") ? 1 : 0;
    }

    /// <summary>Best-effort: <c>SnomedAssociation</c> lives in a different database than the connected one.</summary>
    private async Task MigrateAllergyReactionsAsync(
        SqlConnection conn,
        AdministrationDbContext dbContext,
        bool dryRun,
        List<DrugCatalogMigrationError> errors,
        CancellationToken ct)
    {
        const string TargetTable = "AllergyReactions";

        List<MssqlDrugCatalogMapper.ReactionRow> rows;
        try
        {
            rows = await MssqlDrugCatalogMapper.ReadReactionsAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Best-effort table: a cross-database read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new DrugCatalogMigrationError(TargetTable, "SourceReadSkipped", ex.Message));
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: source read failed ({ex.Message}) — skipped (best-effort), existing rows kept"))
                .ConfigureAwait(false);
            return;
        }

        // Target has a unique filtered index on Term (WHERE IsDeleted = FALSE); the source SNOMED join
        // can legitimately produce the same term for multiple description IDs — keep the first occurrence.
        var seenTerms = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<MssqlDrugCatalogMapper.ReactionRow>(rows.Count);
        var duplicateTerms = 0;
        foreach (var row in rows)
        {
            if (seenTerms.Add(row.Term))
            {
                deduped.Add(row);
            }
            else
            {
                duplicateTerms++;
            }
        }
        rows = deduped;
        if (duplicateTerms > 0)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: skipped {duplicateTerms} duplicate Term row(s) (unique index)"))
                .ConfigureAwait(false);
        }

        if (rows.Count == 0)
        {
            await Console.Out.WriteLineAsync(
                $"[mssql-drug-catalog] {TargetTable}: source returned 0 rows — skipped, existing rows kept")
                .ConfigureAwait(false);
            return;
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: DRY-RUN would replace with {rows.Count} source row(s) (IDs {rows.Min(r => r.Id)}–{rows.Max(r => r.Id)})"))
                .ConfigureAwait(false);
            return;
        }

        try
        {
            await ReplaceAllergyReactionsAsync(dbContext, rows, ct).ConfigureAwait(false);
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: replaced with {rows.Count} row(s)")).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: record the failure and continue with the next table.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new DrugCatalogMigrationError(TargetTable, ex.GetType().Name, ex.Message));
            logger.LogWarning(ex, "[mssql-drug-catalog] {Table} failed: {Message}", TargetTable, ex.Message);
        }
    }

    private async Task MigrateMedicationDoseUnitsAsync(
        SqlConnection conn,
        AdministrationDbContext dbContext,
        bool dryRun,
        List<DrugCatalogMigrationError> errors,
        CancellationToken ct)
    {
        const string TargetTable = "MedicationDoseUnits";

        List<MssqlDrugCatalogMapper.DoseUnitRow> rows;
        try
        {
            rows = await MssqlDrugCatalogMapper.ReadDoseUnitsAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new DrugCatalogMigrationError(TargetTable, "SourceReadFailed", ex.Message));
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: source read failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            return;
        }

        // Target has a unique filtered index on Name (WHERE IsDeleted = FALSE). The legacy
        // MedicationUnitTypes table mixes units, routes and frequencies whose COALESCE'd display name
        // can collide (e.g. multiple "CAPSULE" rows) — keep the first occurrence.
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var dedupedUnits = new List<MssqlDrugCatalogMapper.DoseUnitRow>(rows.Count);
        var duplicateNames = 0;
        foreach (var row in rows)
        {
            if (seenNames.Add(row.Name))
            {
                dedupedUnits.Add(row);
            }
            else
            {
                duplicateNames++;
            }
        }
        rows = dedupedUnits;
        if (duplicateNames > 0)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: skipped {duplicateNames} duplicate Name row(s) (unique index)"))
                .ConfigureAwait(false);
        }

        if (rows.Count == 0)
        {
            await Console.Out.WriteLineAsync(
                $"[mssql-drug-catalog] {TargetTable}: source returned 0 rows — skipped").ConfigureAwait(false);
            return;
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: DRY-RUN would replace with {rows.Count} source row(s) (IDs {rows.Min(r => r.Id)}–{rows.Max(r => r.Id)})"))
                .ConfigureAwait(false);
            return;
        }

        try
        {
            await ReplaceMedicationDoseUnitsAsync(dbContext, rows, ct).ConfigureAwait(false);
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: replaced with {rows.Count} row(s)")).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: record the failure and continue with the next table.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new DrugCatalogMigrationError(TargetTable, ex.GetType().Name, ex.Message));
            logger.LogWarning(ex, "[mssql-drug-catalog] {Table} failed: {Message}", TargetTable, ex.Message);
        }
    }

    /// <summary>
    /// Streams <c>RXNCONSO</c> straight into <c>administration."Drugs"</c> via Npgsql binary COPY —
    /// production RXNCONSO can be millions of rows, so this avoids one round-trip per row. The table is
    /// cleared first so the verb is idempotent/re-runnable; duplicate non-null <c>RxAui</c> values and
    /// blank names are skipped and counted.
    /// </summary>
    private async Task MigrateDrugsAsync(
        SqlConnection conn,
        AdministrationDbContext dbContext,
        bool dryRun,
        List<DrugCatalogMigrationError> errors,
        CancellationToken ct)
    {
        const string TargetTable = "Drugs";

        var connString = dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connString))
        {
            errors.Add(new DrugCatalogMigrationError(
                TargetTable, "MissingTargetConnection", "AdministrationDbContext connection string is empty."));
            await Console.Out.WriteLineAsync(
                $"[mssql-drug-catalog] {TargetTable}: target connection string unavailable — TABLE NOT MIGRATED")
                .ConfigureAwait(false);
            return;
        }

        try
        {
            await using var pg = new NpgsqlConnection(connString);
            await pg.OpenAsync(ct).ConfigureAwait(false);

            var seenRxAuis = new HashSet<string>(StringComparer.Ordinal);
            long imported = 0;
            long skippedBlankName = 0;
            long skippedDuplicateRxAui = 0;

            if (dryRun)
            {
#pragma warning disable CA2100 // DrugsSql is a fixed, code-defined constant — no user input.
                await using var cmd = new SqlCommand(MssqlDrugCatalogMapper.DrugsSql, conn) { CommandTimeout = 300 };
#pragma warning restore CA2100
                await using var rdr = await cmd.ExecuteReaderAsync(
                    System.Data.CommandBehavior.SequentialAccess, ct).ConfigureAwait(false);
                while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                {
                    var row = MssqlDrugCatalogMapper.ReadDrug(rdr);
                    if (string.IsNullOrWhiteSpace(row.Name)) { skippedBlankName++; continue; }
                    if (row.RxAui is not null && !seenRxAuis.Add(row.RxAui)) { skippedDuplicateRxAui++; continue; }
                    imported++;
                    if (imported % 100_000 == 0)
                    {
                        await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                            $"[mssql-drug-catalog] {TargetTable}: {imported} row(s) read so far…")).ConfigureAwait(false);
                    }
                }

                await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                    $"[mssql-drug-catalog] {TargetTable}: DRY-RUN would import {imported} row(s) (skipped {skippedBlankName} blank name, {skippedDuplicateRxAui} duplicate RxAui)"))
                    .ConfigureAwait(false);
                return;
            }

            await using (var del = new NpgsqlCommand($"DELETE FROM {AdministrationDbContext.Schema}.\"Drugs\"", pg))
            {
                await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

#pragma warning disable CA2100 // DrugsSql is a fixed, code-defined constant — no user input.
            await using var insertCmd = new SqlCommand(MssqlDrugCatalogMapper.DrugsSql, conn) { CommandTimeout = 300 };
#pragma warning restore CA2100
            await using var reader = await insertCmd.ExecuteReaderAsync(
                System.Data.CommandBehavior.SequentialAccess, ct).ConfigureAwait(false);

            // The identity Id column is left out of the COPY column list below so Postgres
            // auto-assigns it; the omitted nullable columns default to NULL.
            await using var writer = await pg.BeginBinaryImportAsync(
                "COPY administration.\"Drugs\" (\"Name\", \"RxAui\", \"RxCui\", \"Tty\", \"Sab\", \"Code\", "
                + "\"IsActive\", \"CreatedAtUtc\", \"IsDeleted\") FROM STDIN (FORMAT BINARY)", ct)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var row = MssqlDrugCatalogMapper.ReadDrug(reader);
                if (string.IsNullOrWhiteSpace(row.Name)) { skippedBlankName++; continue; }
                if (row.RxAui is not null && !seenRxAuis.Add(row.RxAui)) { skippedDuplicateRxAui++; continue; }

                await writer.StartRowAsync(ct).ConfigureAwait(false);
                await writer.WriteAsync(Truncate(row.Name, 2048), NpgsqlDbType.Varchar, ct).ConfigureAwait(false);
                await WriteNullable(writer, Truncate(row.RxAui, 12), ct).ConfigureAwait(false);
                await WriteNullable(writer, Truncate(row.RxCui, 12), ct).ConfigureAwait(false);
                await WriteNullable(writer, Truncate(row.Tty, 20), ct).ConfigureAwait(false);
                await WriteNullable(writer, Truncate(row.Sab, 40), ct).ConfigureAwait(false);
                await WriteNullable(writer, Truncate(row.Code, 64), ct).ConfigureAwait(false);
                await writer.WriteAsync(true, NpgsqlDbType.Boolean, ct).ConfigureAwait(false);
                await writer.WriteAsync(DateTime.UtcNow, NpgsqlDbType.TimestampTz, ct).ConfigureAwait(false);
                await writer.WriteAsync(false, NpgsqlDbType.Boolean, ct).ConfigureAwait(false);
                imported++;

                if (imported % 100_000 == 0)
                {
                    await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                        $"[mssql-drug-catalog] {TargetTable}: {imported} row(s) imported so far…")).ConfigureAwait(false);
                }
            }

            await writer.CompleteAsync(ct).ConfigureAwait(false);

            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-drug-catalog] {TargetTable}: imported {imported} row(s) (skipped {skippedBlankName} blank name, {skippedDuplicateRxAui} duplicate RxAui)"))
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: record the failure and continue with the next table.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errors.Add(new DrugCatalogMigrationError(TargetTable, ex.GetType().Name, ex.Message));
            logger.LogWarning(ex, "[mssql-drug-catalog] {Table} failed: {Message}", TargetTable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes all rows in <c>AllergyReactions</c>, inserts the source rows with explicit IDs (preserving
    /// <c>saID</c>), then advances the identity sequence past the highest ID — all in one transaction.
    /// </summary>
    private static async Task ReplaceAllergyReactionsAsync(
        AdministrationDbContext dbContext,
        List<MssqlDrugCatalogMapper.ReactionRow> rows,
        CancellationToken ct)
    {
        var table = $"{AdministrationDbContext.Schema}.\"AllergyReactions\"";

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        // EF1002: `table` is a fixed code-defined constant, never user input — no injection surface.
        // Row values below are parameterized.
#pragma warning disable EF1002
        await dbContext.Database
            .ExecuteSqlRawAsync($"DELETE FROM {table}", ct).ConfigureAwait(false);
#pragma warning restore EF1002

        const string InsertSql =
            "INSERT INTO administration.\"AllergyReactions\" (\"Id\", \"Term\", \"SnomedCode\", \"IsActive\", \"IsDeleted\") "
            + "VALUES (@id, @term, @snomed, TRUE, FALSE)";

        foreach (var row in rows)
        {
            var parameters = new NpgsqlParameter[]
            {
                new("id", row.Id),
                new("term", Truncate(row.Term, 256)!),
                new NpgsqlParameter("snomed", NpgsqlDbType.Text)
                {
                    Value = (object?)Truncate(row.SnomedCode, 32) ?? DBNull.Value,
                },
            };

            await dbContext.Database
                .ExecuteSqlRawAsync(InsertSql, parameters.Cast<object>().ToArray())
                .ConfigureAwait(false);
        }

        // EF1002: `table` is a fixed code-defined constant (see note above), not user input.
#pragma warning disable EF1002
        await dbContext.Database.ExecuteSqlRawAsync(
            $"SELECT setval(pg_get_serial_sequence('{table}', 'Id'), (SELECT MAX(\"Id\") FROM {table}))",
            ct).ConfigureAwait(false);
#pragma warning restore EF1002

        await tx.CommitAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes all rows in <c>MedicationDoseUnits</c>, inserts the source rows with explicit IDs
    /// (preserving <c>mutID</c>), then advances the identity sequence past the highest ID — all in one
    /// transaction.
    /// </summary>
    private static async Task ReplaceMedicationDoseUnitsAsync(
        AdministrationDbContext dbContext,
        List<MssqlDrugCatalogMapper.DoseUnitRow> rows,
        CancellationToken ct)
    {
        var table = $"{AdministrationDbContext.Schema}.\"MedicationDoseUnits\"";

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

#pragma warning disable EF1002
        await dbContext.Database
            .ExecuteSqlRawAsync($"DELETE FROM {table}", ct).ConfigureAwait(false);
#pragma warning restore EF1002

        const string InsertSql =
            "INSERT INTO administration.\"MedicationDoseUnits\" (\"Id\", \"Name\", \"IsActive\", \"IsDeleted\") "
            + "VALUES (@id, @name, TRUE, FALSE)";

        foreach (var row in rows)
        {
            var parameters = new NpgsqlParameter[]
            {
                new("id", row.Id),
                new("name", Truncate(row.Name, 64)!),
            };

            await dbContext.Database
                .ExecuteSqlRawAsync(InsertSql, parameters.Cast<object>().ToArray())
                .ConfigureAwait(false);
        }

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
        List<DrugCatalogMigrationError> errors, bool dryRun, CancellationToken ct)
    {
        var suffix = dryRun ? "dry-run" : "live";
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var path = $"drug-catalog-migration-errors-{suffix}-{timestamp}.json";
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(errors, ErrorFileOptions), ct)
            .ConfigureAwait(false);
        await Console.Out.WriteLineAsync(
            $"[mssql-drug-catalog] {errors.Count} error(s) written to {path}").ConfigureAwait(false);
    }

    private static string? Truncate(string? value, int max)
    {
        if (value is null) return null;
        return value.Length <= max ? value : value[..max];
    }

    private static async Task WriteNullable(NpgsqlBinaryImporter writer, string? value, CancellationToken ct)
    {
        if (value is null)
        {
            await writer.WriteNullAsync(ct).ConfigureAwait(false);
        }
        else
        {
            await writer.WriteAsync(value, NpgsqlDbType.Varchar, ct).ConfigureAwait(false);
        }
    }

    private sealed record DrugCatalogMigrationError(string Table, string ErrorType, string Message);
}
