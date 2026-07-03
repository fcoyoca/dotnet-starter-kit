using System.Globalization;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

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

        // Clinical lists reference patients by pUniqueID → Patient.LegacyUniqueId, populated by the
        // upsert loop above (this run or a prior one). No symmetric key needed — these four tables
        // are entirely plaintext.
        await MigrateClinicalListsAsync(conn, tenant, dryRun, ct).ConfigureAwait(false);

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
    private static async Task<List<(CreatePatientCommand Cmd, string PId)>> ReadBatchAsync(
        SqlConnection conn, int offset, int batchSize, CancellationToken ct)
    {
        var results = new List<(CreatePatientCommand, string)>();
        await using var cmd = new SqlCommand(
            MssqlPatientMapper.BuildSelectQuery(offset, batchSize), conn);
        cmd.CommandTimeout = 120;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var pIdOrd = reader.GetOrdinal("pID");
            var pId = await reader.IsDBNullAsync(pIdOrd, ct).ConfigureAwait(false)
                ? string.Empty
                : reader.GetString(pIdOrd).Trim();
            results.Add((MssqlPatientMapper.Map(reader), pId));
        }
        return results;
    }
#pragma warning restore CA2100

    private static void ValidateDryRun(
        CreatePatientCommand cmd, string pId, List<MigrationError> errors)
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
            // MssqlPatientMapper always sets PatientCode to "P-{pId}"; never null here even
            // though CreatePatientCommand.PatientCode is nullable for the dashboard's create flow.
            errors.Add(new MigrationError(
                pId, cmd.PatientCode!, "ValidationFailed", string.Join("; ", fieldErrors)));
        }
    }

    private async Task<UpsertResult> UpsertAsync(
        AppTenantInfo tenant, CreatePatientCommand cmd, string pId,
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
            errors.Add(new MigrationError(pId, cmd.PatientCode!, ex.GetType().Name, ex.Message));
            logger.LogWarning(ex,
                "[mssql-migration] row pID={PId} PatientCode={PatientCode} failed: {Message}",
                pId, cmd.PatientCode, ex.Message);
            return UpsertResult.Skipped;
        }
    }

    /// <summary>
    /// Migrates the four clinical-list tables (PatientAllergies, PatientMedications, PatientNotes,
    /// MedicationReconciledDates) for patients resolvable via <c>Patient.LegacyUniqueId</c> — populated
    /// by the upsert loop above (this run or a prior one).
    ///
    /// <para>Uses direct <see cref="PatientDbContext"/> access (not Mediator), unlike the patient
    /// upsert loop above: <c>MarkMedicationsReconciledCommand</c> always stamps <c>DateTime.UtcNow</c>
    /// and has no parameter for a historical reconciled date, and this is a bulk historical-data import
    /// rather than a live user action — the create-active-allergy-clears-no-known-allergies side effect
    /// from the live command handlers intentionally does not apply here. Legacy data is mirrored as-is,
    /// including any inconsistency between the patient flags and the child rows; the patient flags
    /// themselves are already projected independently by <see cref="MssqlPatientMapper"/>.</para>
    /// <para><b>Idempotency:</b> before inserting each table's rows for patients resolved in this run,
    /// any previously-migrated rows for those same patients are deleted first (delete-by-patient-ids),
    /// so re-running this verb does not duplicate clinical-list rows.</para>
    /// <para>None of these four source tables have encrypted columns — no symmetric key needed.</para>
    /// </summary>
    private async Task MigrateClinicalListsAsync(
        SqlConnection conn, AppTenantInfo tenant, bool dryRun, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        // Set the Finbuckle tenant context so PatientDbContext resolves the same physical database
        // the patient upsert phase above just wrote to.
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        Dictionary<long, Guid> patientByLegacyId = await dbContext.Patients
            .Where(p => p.LegacyUniqueId != null)
            .ToDictionaryAsync(p => p.LegacyUniqueId!.Value, p => p.Id, ct)
            .ConfigureAwait(false);

        await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"[mssql-migration] clinical-lists: {patientByLegacyId.Count} patient(s) resolvable via LegacyUniqueId"))
            .ConfigureAwait(false);

        await MigrateAllergiesAsync(conn, dbContext, patientByLegacyId, dryRun, ct).ConfigureAwait(false);
        await MigrateMedicationsAsync(conn, dbContext, patientByLegacyId, dryRun, ct).ConfigureAwait(false);
        await MigrateNotesAsync(conn, dbContext, patientByLegacyId, dryRun, ct).ConfigureAwait(false);
        await MigrateReconciledDatesAsync(conn, dbContext, patientByLegacyId, dryRun, ct).ConfigureAwait(false);
    }

    private async Task MigrateAllergiesAsync(
        SqlConnection conn,
        PatientDbContext dbContext,
        Dictionary<long, Guid> patientByLegacyId,
        bool dryRun,
        CancellationToken ct)
    {
        const string TargetTable = "PatientAllergies";

        List<MssqlClinicalListMapper.AllergyRow> rows;
        try
        {
            rows = await MssqlClinicalListMapper.ReadAllergiesAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: source read failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} source read failed: {Message}", TargetTable, ex.Message);
            return;
        }

        var resolved = new List<(MssqlClinicalListMapper.AllergyRow Row, Guid PatientId)>(rows.Count);
        var skipped = 0;
        foreach (var row in rows)
        {
            if (patientByLegacyId.TryGetValue(row.PatientUniqueId, out var patientId))
            {
                resolved.Add((row, patientId));
            }
            else
            {
                skipped++;
            }
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: DRY-RUN read {rows.Count} row(s), {resolved.Count} resolvable, {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
            return;
        }

        var patientIds = resolved.Select(r => r.PatientId).Distinct().ToArray();

        try
        {
            await DeleteExistingForPatientsAsync(dbContext, TargetTable, patientIds, ct).ConfigureAwait(false);

            var added = 0;
            foreach (var (row, patientId) in resolved)
            {
                var drugName = string.IsNullOrWhiteSpace(row.DrugName) ? "(unknown)" : row.DrugName;
                dbContext.PatientAllergies.Add(PatientAllergy.Create(
                    patientId,
                    drugName,
                    row.RxAui,
                    row.Reaction,
                    row.Comments,
                    row.DateNoted ?? row.CreatedDate ?? DateTime.UtcNow,
                    row.Active,
                    createdByUserId: null,
                    createdByName: "BackChart migration"));
                added++;
                if (added % 500 == 0)
                {
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                    dbContext.ChangeTracker.Clear();
                }
            }
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();

            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: migrated {added} row(s), {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a write failure must not abort the rest of the migration.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            dbContext.ChangeTracker.Clear();
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: write failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} write failed: {Message}", TargetTable, ex.Message);
        }
    }

    private async Task MigrateMedicationsAsync(
        SqlConnection conn,
        PatientDbContext dbContext,
        Dictionary<long, Guid> patientByLegacyId,
        bool dryRun,
        CancellationToken ct)
    {
        const string TargetTable = "PatientMedications";

        List<MssqlClinicalListMapper.MedicationRow> rows;
        try
        {
            rows = await MssqlClinicalListMapper.ReadMedicationsAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: source read failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} source read failed: {Message}", TargetTable, ex.Message);
            return;
        }

        var resolved = new List<(MssqlClinicalListMapper.MedicationRow Row, Guid PatientId)>(rows.Count);
        var skipped = 0;
        foreach (var row in rows)
        {
            if (patientByLegacyId.TryGetValue(row.PatientUniqueId, out var patientId))
            {
                resolved.Add((row, patientId));
            }
            else
            {
                skipped++;
            }
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: DRY-RUN read {rows.Count} row(s), {resolved.Count} resolvable, {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
            return;
        }

        var patientIds = resolved.Select(r => r.PatientId).Distinct().ToArray();

        try
        {
            await DeleteExistingForPatientsAsync(dbContext, TargetTable, patientIds, ct).ConfigureAwait(false);

            var added = 0;
            foreach (var (row, patientId) in resolved)
            {
                var drugName = string.IsNullOrWhiteSpace(row.DrugName) ? "(unknown)" : row.DrugName;
                dbContext.PatientMedications.Add(PatientMedication.Create(
                    patientId,
                    drugName,
                    row.RxAui,
                    rxCode: null, // legacy PatientMedications has no RXCUI column — always null for migrated rows.
                    row.Ndc,
                    row.Prescriber,
                    row.StartDate ?? row.CreatedDate ?? DateTime.UtcNow,
                    row.EndDate,
                    row.DoseValue,
                    row.DoseUnitId,
                    row.DosePeriodValue,
                    row.DosePeriodUnit,
                    row.Instructions,
                    row.Indication,
                    row.Active,
                    createdByUserId: null,
                    createdByName: "BackChart migration"));
                added++;
                if (added % 500 == 0)
                {
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                    dbContext.ChangeTracker.Clear();
                }
            }
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();

            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: migrated {added} row(s), {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a write failure must not abort the rest of the migration.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            dbContext.ChangeTracker.Clear();
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: write failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} write failed: {Message}", TargetTable, ex.Message);
        }
    }

    private async Task MigrateNotesAsync(
        SqlConnection conn,
        PatientDbContext dbContext,
        Dictionary<long, Guid> patientByLegacyId,
        bool dryRun,
        CancellationToken ct)
    {
        const string TargetTable = "PatientNotes";

        List<MssqlClinicalListMapper.NoteRow> rows;
        try
        {
            rows = await MssqlClinicalListMapper.ReadNotesAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: source read failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} source read failed: {Message}", TargetTable, ex.Message);
            return;
        }

        var resolved = new List<(MssqlClinicalListMapper.NoteRow Row, Guid PatientId)>(rows.Count);
        var skipped = 0;
        foreach (var row in rows)
        {
            if (patientByLegacyId.TryGetValue(row.PatientUniqueId, out var patientId))
            {
                resolved.Add((row, patientId));
            }
            else
            {
                skipped++;
            }
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: DRY-RUN read {rows.Count} row(s), {resolved.Count} resolvable, {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
            return;
        }

        var patientIds = resolved.Select(r => r.PatientId).Distinct().ToArray();

        try
        {
            await DeleteExistingForPatientsAsync(dbContext, TargetTable, patientIds, ct).ConfigureAwait(false);

            var added = 0;
            foreach (var (row, patientId) in resolved)
            {
                var name = string.IsNullOrWhiteSpace(row.Name) ? "(unknown)" : row.Name;
                var note = PatientNote.Create(
                    patientId,
                    name,
                    row.Description,
                    row.MedicalAlert,
                    createdByUserId: null,
                    createdByName: "BackChart migration");
                if (row.Deleted)
                {
                    note.Delete("BackChart migration");
                }
                dbContext.PatientNotes.Add(note);
                added++;
                if (added % 500 == 0)
                {
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                    dbContext.ChangeTracker.Clear();
                }
            }
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();

            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: migrated {added} row(s), {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a write failure must not abort the rest of the migration.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            dbContext.ChangeTracker.Clear();
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: write failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} write failed: {Message}", TargetTable, ex.Message);
        }
    }

    private async Task MigrateReconciledDatesAsync(
        SqlConnection conn,
        PatientDbContext dbContext,
        Dictionary<long, Guid> patientByLegacyId,
        bool dryRun,
        CancellationToken ct)
    {
        const string TargetTable = "MedicationReconciledDates";

        List<MssqlClinicalListMapper.ReconciledDateRow> rows;
        try
        {
            rows = await MssqlClinicalListMapper.ReadReconciledDatesAsync(conn, ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a single source-read failure must not abort the rest.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: source read failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} source read failed: {Message}", TargetTable, ex.Message);
            return;
        }

        var resolved = new List<(MssqlClinicalListMapper.ReconciledDateRow Row, Guid PatientId)>(rows.Count);
        var skipped = 0;
        foreach (var row in rows)
        {
            if (patientByLegacyId.TryGetValue(row.PatientUniqueId, out var patientId))
            {
                resolved.Add((row, patientId));
            }
            else
            {
                skipped++;
            }
        }

        if (dryRun)
        {
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: DRY-RUN read {rows.Count} row(s), {resolved.Count} resolvable, {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
            return;
        }

        var patientIds = resolved.Select(r => r.PatientId).Distinct().ToArray();

        try
        {
            await DeleteExistingForPatientsAsync(dbContext, TargetTable, patientIds, ct).ConfigureAwait(false);

            var added = 0;
            foreach (var (row, patientId) in resolved)
            {
                dbContext.MedicationReconciledDates.Add(MedicationReconciledDate.Create(
                    patientId,
                    row.Date ?? DateTime.UtcNow,
                    createdByUserId: null,
                    createdByName: "BackChart migration"));
                added++;
                if (added % 500 == 0)
                {
                    await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                    dbContext.ChangeTracker.Clear();
                }
            }
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();

            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: migrated {added} row(s), {skipped} skipped (no matching patient)"))
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Per-table isolation: a write failure must not abort the rest of the migration.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            dbContext.ChangeTracker.Clear();
            await Console.Out.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
                $"[mssql-migration] {TargetTable}: write failed ({ex.Message}) — TABLE NOT MIGRATED"))
                .ConfigureAwait(false);
            logger.LogWarning(ex, "[mssql-migration] {Table} write failed: {Message}", TargetTable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes previously-migrated rows for the given patients before re-inserting — the idempotency
    /// mechanism for the clinical-list phase (these tables have no legacy-row-id column to dedupe on,
    /// since the target entities use <c>Guid</c> PKs, so replace-by-patient is used instead).
    /// </summary>
    private static async Task DeleteExistingForPatientsAsync(
        PatientDbContext dbContext, string table, Guid[] patientIds, CancellationToken ct)
    {
        if (patientIds.Length == 0) return;

        var sql = $"""DELETE FROM {PatientDbContext.Schema}."{table}" WHERE "PatientId" = ANY(@ids)""";
        // EF1002: `table` is always one of the four fixed constants declared above — never user input.
#pragma warning disable EF1002
        await dbContext.Database
            .ExecuteSqlRawAsync(sql, [new NpgsqlParameter("ids", patientIds)], ct)
            .ConfigureAwait(false);
#pragma warning restore EF1002
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

    private sealed record MigrationError(string PId, string PatientCode, string ErrorType, string Message);
}
