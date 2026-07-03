using System.Globalization;
using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Reads BackChart's four per-patient clinical-list tables
/// (<c>PatientAllergies</c>, <c>PatientMedications</c>, <c>PatientNotes</c>,
/// <c>MedicationReconciledDates</c>) — all in the same <c>Bronston</c> database
/// <see cref="MssqlPatientMapper"/> already connects to for <c>Patients</c>.
///
/// <para>
/// None of these tables have encrypted (<c>varbinary</c>) columns — confirmed via
/// <c>INFORMATION_SCHEMA.COLUMNS</c> against the live legacy database. No symmetric-key
/// ceremony is required for these reads.
/// </para>
/// <para>
/// <c>paID</c>/<c>paPatientUniqueID</c>/<c>paRXAUI</c> are SQL Server <c>numeric</c>, not
/// <c>int</c> — read via <c>Convert.ToInt64</c>/<c>Convert.ToString</c>, matching the idiom
/// <see cref="MssqlPatientMapper"/> already uses for the numeric <c>pUniqueID</c> column.
/// </para>
/// </summary>
internal static class MssqlClinicalListMapper
{
    internal sealed record AllergyRow(
        long Id,
        long PatientUniqueId,
        string? DrugName,
        string? RxAui,
        string? Reaction,
        string? Comments,
        DateTime? DateNoted,
        DateTime? CreatedDate,
        bool Active);

    internal sealed record MedicationRow(
        int Id,
        long PatientUniqueId,
        string? RxAui,
        string? Ndc,
        string? DrugName,
        string? Prescriber,
        DateTime? StartDate,
        DateTime? EndDate,
        int? DoseValue,
        int? DoseUnitId,
        int? DosePeriodValue,
        string? DosePeriodUnit,
        string? Instructions,
        string? Indication,
        DateTime? CreatedDate,
        bool Active);

    internal sealed record NoteRow(
        int Id,
        long PatientUniqueId,
        string? Name,
        string? Description,
        bool Deleted,
        bool MedicalAlert);

    internal sealed record ReconciledDateRow(
        int Id,
        long PatientUniqueId,
        DateTime? Date);

    internal const string AllergiesSql = """
        SELECT paID, paPatientUniqueID, paDrugName, paRXAUI, paReaction, paComments,
               paDateNoted, paCreatedDate, paActive
        FROM dbo.PatientAllergies
        """;

    internal const string MedicationsSql = """
        SELECT pmID, pmPatientUniqueID, pmRXAUI, pmNDC, pmDrugName, pmPrescriber,
               pmStartDate, pmEndDate, pmDoseValue, pmDoseUnitID, pmDosePeriodValue,
               pmDosePeriodUnit, pmInstructions, pmIndication, pmCreatedDate, pmActive
        FROM dbo.PatientMedications
        """;

    internal const string NotesSql = """
        SELECT pnID, pnPatientUniqueID, pnName, pnDescription, pnDeleted, pnMedicalAlert
        FROM dbo.PatientNotes
        """;

    internal const string ReconciledDatesSql = """
        SELECT mrdID, mrdPatientUniqueID, mrdDate
        FROM dbo.MedicationReconciledDates
        """;

    internal static async Task<List<AllergyRow>> ReadAllergiesAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);
        var rows = new List<AllergyRow>();
#pragma warning disable CA2100 // AllergiesSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(AllergiesSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            rows.Add(new AllergyRow(
                Id: GetLong(rdr, "paID") ?? 0,
                PatientUniqueId: GetLong(rdr, "paPatientUniqueID") ?? 0,
                DrugName: GetString(rdr, "paDrugName"),
                RxAui: GetNumericAsString(rdr, "paRXAUI"),
                Reaction: GetString(rdr, "paReaction"),
                Comments: GetString(rdr, "paComments"),
                DateNoted: GetDate(rdr, "paDateNoted"),
                CreatedDate: GetDate(rdr, "paCreatedDate"),
                Active: GetBoolOrDefault(rdr, "paActive", true)));
        }
        return rows;
    }

    internal static async Task<List<MedicationRow>> ReadMedicationsAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);
        var rows = new List<MedicationRow>();
#pragma warning disable CA2100 // MedicationsSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(MedicationsSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            rows.Add(new MedicationRow(
                Id: GetInt(rdr, "pmID") ?? 0,
                PatientUniqueId: GetLong(rdr, "pmPatientUniqueID") ?? 0,
                RxAui: GetString(rdr, "pmRXAUI"),
                Ndc: GetString(rdr, "pmNDC"),
                DrugName: GetString(rdr, "pmDrugName"),
                Prescriber: GetString(rdr, "pmPrescriber"),
                StartDate: GetDate(rdr, "pmStartDate"),
                EndDate: GetDate(rdr, "pmEndDate"),
                DoseValue: GetInt(rdr, "pmDoseValue"),
                DoseUnitId: GetInt(rdr, "pmDoseUnitID"),
                DosePeriodValue: GetInt(rdr, "pmDosePeriodValue"),
                DosePeriodUnit: GetString(rdr, "pmDosePeriodUnit"),
                Instructions: GetString(rdr, "pmInstructions"),
                Indication: GetString(rdr, "pmIndication"),
                CreatedDate: GetDate(rdr, "pmCreatedDate"),
                Active: GetBoolOrDefault(rdr, "pmActive", true)));
        }
        return rows;
    }

    internal static async Task<List<NoteRow>> ReadNotesAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);
        var rows = new List<NoteRow>();
#pragma warning disable CA2100 // NotesSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(NotesSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            rows.Add(new NoteRow(
                Id: GetInt(rdr, "pnID") ?? 0,
                PatientUniqueId: GetLong(rdr, "pnPatientUniqueID") ?? 0,
                Name: GetString(rdr, "pnName"),
                Description: GetString(rdr, "pnDescription"),
                Deleted: GetBoolOrDefault(rdr, "pnDeleted", false),
                MedicalAlert: GetBoolOrDefault(rdr, "pnMedicalAlert", false)));
        }
        return rows;
    }

    internal static async Task<List<ReconciledDateRow>> ReadReconciledDatesAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);
        var rows = new List<ReconciledDateRow>();
#pragma warning disable CA2100 // ReconciledDatesSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(ReconciledDatesSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            rows.Add(new ReconciledDateRow(
                Id: GetInt(rdr, "mrdID") ?? 0,
                PatientUniqueId: GetLong(rdr, "mrdPatientUniqueID") ?? 0,
                Date: GetDate(rdr, "mrdDate")));
        }
        return rows;
    }

    private static string? GetString(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        var val = r.GetString(ord).Trim();
        return val.Length == 0 ? null : val;
    }

    /// <summary>Reads a SQL Server <c>numeric</c> column and formats it as a plain integer string
    /// (drops any decimal places) — used for <c>paRXAUI</c>, which the source stores as
    /// <c>numeric</c> even though it represents an RxNorm AUI code. The column is never
    /// <c>NULL</c> in the live legacy data; <c>0</c> is instead used as the "no RxAUI matched"
    /// sentinel (7 of 996 rows, verified against the live database), so it is mapped to
    /// <c>null</c> here rather than the misleading literal string <c>"0"</c>.</summary>
    private static string? GetNumericAsString(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        var value = Convert.ToInt64(r.GetValue(ord), CultureInfo.InvariantCulture);
        return value == 0 ? null : value.ToString(CultureInfo.InvariantCulture);
    }

    private static DateTime? GetDate(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        var dt = r.GetDateTime(ord);
        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
    }

    private static int? GetInt(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? null : Convert.ToInt32(r.GetValue(ord), CultureInfo.InvariantCulture);
    }

    private static long? GetLong(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? null : Convert.ToInt64(r.GetValue(ord), CultureInfo.InvariantCulture);
    }

    private static bool GetBoolOrDefault(SqlDataReader r, string col, bool defaultValue)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return defaultValue;
        // bit columns come back as bool already; be defensive in case a source column is int.
        return Convert.ToBoolean(r.GetValue(ord), CultureInfo.InvariantCulture);
    }
}
