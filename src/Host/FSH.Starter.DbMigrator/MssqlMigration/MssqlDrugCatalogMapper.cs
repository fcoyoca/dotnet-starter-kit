using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Reads the legacy RxNorm/SNOMED/dose-unit source rows that seed the Administration module's
/// drug catalog (<c>Drugs</c>, <c>AllergyReactions</c>, <c>MedicationDoseUnits</c>).
///
/// <para>
/// <c>RXNCONSO</c>, <c>Snomed</c> and <c>MedicationUnitTypes</c> live in
/// <c>BronstonAuthenticatingDB</c> (the same database <see cref="MssqlLookupMapper"/> reads from).
/// <c>SnomedAssociation</c> (singular) lives in the sibling <c>Bronston</c> patient database, so
/// <see cref="ReactionsSql"/> uses a hardcoded three-part cross-database reference — both databases
/// live on the same SQL Server instance in this legacy system, so this works without a second
/// connection string. Verified against the live legacy databases 2026-07-03.
/// </para>
/// </summary>
internal static class MssqlDrugCatalogMapper
{
    /// <summary>One RXNCONSO row. <c>RxAui</c>/<c>RxCui</c> are read as strings even though the
    /// source columns are <c>int</c> — the target Drug entity stores them as short varchar codes.</summary>
    internal sealed record DrugRow(string? RxAui, string? RxCui, string Name, string? Tty, string? Sab, string? Code);

    /// <summary>One curated allergy-reaction row (SnomedAssociation × Snomed, saIsReaction = 1).</summary>
    internal sealed record ReactionRow(int Id, string? SnomedCode, string Term);

    /// <summary>One MedicationUnitTypes row.</summary>
    internal sealed record DoseUnitRow(int Id, string Name);

    internal const string DrugsSql = "SELECT RXAUI, RXCUI, STR, TTY, SAB, CODE FROM dbo.RXNCONSO";

    /// <summary>
    /// Cross-database join: <c>SnomedAssociation</c> is in <c>Bronston</c>, <c>Snomed</c> is in the
    /// connected database (<c>BronstonAuthenticatingDB</c>). Marked best-effort by the runner — if the
    /// sibling database name differs or isn't reachable in a given environment, this table is skipped
    /// rather than aborting the whole migration.
    /// </summary>
    internal const string ReactionsSql = """
        SELECT sa.saID, s.snoConceptID, s.snoTerm
        FROM [Bronston].dbo.SnomedAssociation sa
        JOIN dbo.Snomed s ON s.snoDescriptionID = sa.saSnomedDescriptionID
        WHERE sa.saIsReaction = 1
        """;

    internal const string DoseUnitsSql = """
        SELECT mutID, COALESCE(NULLIF(mutCDISCSubmissionValue, ''), mutNCIPreferredTerm) AS UnitName
        FROM dbo.MedicationUnitTypes
        """;

    /// <summary>Reads one RXNCONSO row. Ordinal reads only — compatible with <c>CommandBehavior.SequentialAccess</c>.</summary>
    internal static DrugRow ReadDrug(SqlDataReader rdr) => new(
        RxAui: rdr.IsDBNull(0) ? null : Convert.ToString(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        RxCui: rdr.IsDBNull(1) ? null : Convert.ToString(rdr.GetValue(1), System.Globalization.CultureInfo.InvariantCulture),
        Name: rdr.IsDBNull(2) ? string.Empty : Convert.ToString(rdr.GetValue(2), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
        Tty: rdr.IsDBNull(3) ? null : Convert.ToString(rdr.GetValue(3), System.Globalization.CultureInfo.InvariantCulture),
        Sab: rdr.IsDBNull(4) ? null : Convert.ToString(rdr.GetValue(4), System.Globalization.CultureInfo.InvariantCulture),
        Code: rdr.IsDBNull(5) ? null : Convert.ToString(rdr.GetValue(5), System.Globalization.CultureInfo.InvariantCulture));

    private static ReactionRow ReadReaction(SqlDataReader rdr) => new(
        Id: Convert.ToInt32(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        SnomedCode: rdr.IsDBNull(1) ? null : Convert.ToString(rdr.GetValue(1), System.Globalization.CultureInfo.InvariantCulture),
        Term: rdr.IsDBNull(2) ? string.Empty : Convert.ToString(rdr.GetValue(2), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);

    private static DoseUnitRow ReadDoseUnit(SqlDataReader rdr) => new(
        Id: Convert.ToInt32(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        Name: rdr.IsDBNull(1) ? string.Empty : Convert.ToString(rdr.GetValue(1), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);

    /// <summary>Reads every curated allergy-reaction row. Blank terms are skipped. The connection must already be open.</summary>
    internal static async Task<List<ReactionRow>> ReadReactionsAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);

        var rows = new List<ReactionRow>();
#pragma warning disable CA2100 // ReactionsSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(ReactionsSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = ReadReaction(reader);
            if (string.IsNullOrWhiteSpace(row.Term)) continue;
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>Reads every dose-unit row. Blank names are skipped. The connection must already be open.</summary>
    internal static async Task<List<DoseUnitRow>> ReadDoseUnitsAsync(SqlConnection conn, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);

        var rows = new List<DoseUnitRow>();
#pragma warning disable CA2100 // DoseUnitsSql is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(DoseUnitsSql, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = ReadDoseUnit(reader);
            if (string.IsNullOrWhiteSpace(row.Name)) continue;
            rows.Add(row);
        }
        return rows;
    }
}
