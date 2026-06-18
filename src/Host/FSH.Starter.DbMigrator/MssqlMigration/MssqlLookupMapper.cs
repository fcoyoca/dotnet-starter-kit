using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Describes how each BackChart/BronstonChiro lookup table maps onto a target
/// Administration-module table, and reads source rows preserving their original integer IDs.
///
/// <para>
/// Lookup tables are stored as plaintext in the source database (unlike the patient PHI columns),
/// so no symmetric-key decryption is required here.
/// </para>
/// </summary>
internal static class MssqlLookupMapper
{
    /// <summary>A single source lookup row with its original ID preserved.</summary>
    internal sealed record LookupRow(int Id, string Name, string? SnomedCode);

    /// <summary>Maps one source lookup table to its Administration-module counterpart.</summary>
    /// <param name="TargetTable">Unquoted target table name in the <c>administration</c> schema.</param>
    /// <param name="SourceQuery">MSSQL SELECT returning <c>Id</c>, <c>Name</c> and (optionally) <c>SnomedCode</c>.</param>
    /// <param name="HasSnomedCode">When true, the target table has a <c>SnomedCode</c> column to populate.</param>
    /// <param name="BestEffort">
    /// When true the source table name is unconfirmed; if the read fails the runner skips this table and
    /// leaves the existing (seeded) target rows untouched instead of aborting the whole migration.
    /// </param>
    internal sealed record LookupTableMap(
        string TargetTable,
        string SourceQuery,
        bool HasSnomedCode = false,
        bool BestEffort = false);

    /// <summary>
    /// The six lookup tables, in dependency-safe order. Source table/column names were recovered from the
    /// legacy Flex AS3 client models (Races/Ethnicity/Languages/lupSmokingStatuses/PreferedContactMethods)
    /// and SHOULD be re-verified against the live BronstonChiro database before a production run.
    /// </summary>
    public static IReadOnlyList<LookupTableMap> Tables { get; } =
    [
        new("Races",
            "SELECT racID AS Id, racName AS Name FROM dbo.Races"),
        new("Ethnicities",
            "SELECT ethID AS Id, ethName AS Name FROM dbo.Ethnicity"),
        new("Languages",
            "SELECT lanID AS Id, lanName AS Name FROM dbo.Languages"),
        new("SmokingStatuses",
            "SELECT lssID AS Id, lssDescription AS Name, lssSNOMED_Code AS SnomedCode FROM dbo.lupSmokingStatuses",
            HasSnomedCode: true),
        new("PreferredContactMethods",
            "SELECT pcmID AS Id, pcmName AS Name FROM dbo.PreferedContactMethods"),
        // BronstonChiro has no AS3 model for referral types and pReferralTypeID is a TinyInt — the source
        // table name below is a best-effort guess. Correct it once confirmed on the live DB; until then the
        // runner skips this table on failure and keeps the seeded ReferralTypes rows.
        new("ReferralTypes",
            "SELECT rtfID AS Id, rtfName AS Name FROM dbo.lupReferralTypes",
            BestEffort: true),
    ];

    /// <summary>Reads every row of one source lookup query. The connection must already be open.</summary>
    public static async Task<List<LookupRow>> ReadAsync(
        SqlConnection conn, LookupTableMap map, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(conn);
        ArgumentNullException.ThrowIfNull(map);

        var rows = new List<LookupRow>();
#pragma warning disable CA2100 // SourceQuery is a fixed, code-defined constant — no user input.
        await using var cmd = new SqlCommand(map.SourceQuery, conn) { CommandTimeout = 120 };
#pragma warning restore CA2100
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var id = Convert.ToInt32(reader["Id"], System.Globalization.CultureInfo.InvariantCulture);
            var name = reader["Name"] is string s ? s.Trim() : reader["Name"]?.ToString()?.Trim();
            if (string.IsNullOrEmpty(name)) continue; // skip blank lookup values

            string? snomed = null;
            if (map.HasSnomedCode)
            {
                var ord = reader.GetOrdinal("SnomedCode");
                snomed = await reader.IsDBNullAsync(ord, ct).ConfigureAwait(false)
                    ? null
                    : reader.GetString(ord).Trim();
                if (snomed?.Length == 0) snomed = null;
            }

            rows.Add(new LookupRow(id, name, snomed));
        }
        return rows;
    }
}
