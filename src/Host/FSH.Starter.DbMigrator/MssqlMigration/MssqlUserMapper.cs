using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Maps a row from BackChart/BronstonChiro <c>dbo.Users</c> (the staff/provider table) to a
/// <see cref="SourceUser"/>. FirstName/LastName/Username/Email are stored as <c>varbinary</c> with the
/// same SQL Server symmetric key as the patient PHI columns, so the key must be open on the connection
/// (see <see cref="OpenKeyStatement"/>) before running <see cref="SelectQuery"/>.
/// </summary>
internal static class MssqlUserMapper
{
    public const string OpenKeyStatement =
        "OPEN SYMMETRIC KEY [SecretTable_SecretData_Key] " +
        "DECRYPTION BY CERTIFICATE [cert_SecretTable_SecretData_Key]";

    public const string CloseKeyStatement =
        "CLOSE SYMMETRIC KEY [SecretTable_SecretData_Key]";

    /// <summary>A decrypted source user. <see cref="LegacyUserId"/> is the source <c>uID</c>.</summary>
    internal sealed record SourceUser(
        int LegacyUserId,
        string? FirstName,
        string? LastName,
        string? UserName,
        string? Email,
        bool IsSuperUser,
        bool IsDoctor);

    /// <summary>
    /// Selects active (non-deleted) users, decrypting the varbinary identity columns inline.
    /// The symmetric key must already be open on the connection.
    /// </summary>
    public const string SelectQuery =
        """
        SELECT
            u.uID                                                          AS uID,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(u.uFirstName))             AS uFirstName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(u.uLastName))              AS uLastName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(u.uUsername))              AS uUsername,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(u.uEmail))                 AS uEmail,
            u.uSuperUser                                                   AS uSuperUser,
            u.uDoctor                                                      AS uDoctor
        FROM dbo.Users u
        WHERE u.uDeletedDate IS NULL
        ORDER BY u.uID
        """;

    public static SourceUser Map(SqlDataReader r)
    {
        ArgumentNullException.ThrowIfNull(r);

        return new SourceUser(
            LegacyUserId: Convert.ToInt32(r["uID"], System.Globalization.CultureInfo.InvariantCulture),
            FirstName: GetString(r, "uFirstName"),
            LastName: GetString(r, "uLastName"),
            UserName: GetString(r, "uUsername"),
            Email: GetString(r, "uEmail"),
            IsSuperUser: GetBoolOrDefault(r, "uSuperUser", false),
            IsDoctor: GetBoolOrDefault(r, "uDoctor", false));
    }

    private static string? GetString(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        var val = r.GetString(ord).Trim();
        return val.Length == 0 ? null : val;
    }

    private static bool GetBoolOrDefault(SqlDataReader r, string col, bool defaultValue)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? defaultValue : r.GetBoolean(ord);
    }
}
