using FSH.Modules.Patient.Contracts.v1.Patients;
using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

/// <summary>
/// Maps a row from BackChart's dbo.Patients table to a <see cref="CreatePatientCommand"/>.
///
/// <para>
/// The source database stores most PHI columns as <c>varbinary</c> using SQL Server symmetric-key
/// encryption (<c>DECRYPTBYKEY</c>).  The caller must open the symmetric key on the connection
/// before executing the query returned by <see cref="BuildSelectQuery"/>.
/// </para>
/// </summary>
internal static class MssqlPatientMapper
{
    public const string OpenKeyStatement =
        "OPEN SYMMETRIC KEY [SecretTable_SecretData_Key] " +
        "DECRYPTION BY CERTIFICATE [cert_SecretTable_SecretData_Key]";

    public const string CloseKeyStatement =
        "CLOSE SYMMETRIC KEY [SecretTable_SecretData_Key]";

    /// <summary>
    /// Builds the paginated SELECT that decrypts every varbinary column inline.
    /// The symmetric key must already be open on the connection.
    /// </summary>
    public static string BuildSelectQuery(int offset, int batchSize) =>
        $"""
        SELECT
            p.pID,
            p.pIsActive,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pFirstName))          AS pFirstName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pLastName))           AS pLastName,
            p.pMI,
            TRY_CONVERT(date, CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pDOB)))   AS pDOB,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pSSN))                AS pSSN,
            p.pSex,
            p.pMaritalStatus,
            p.pMinor,
            p.pRaceID,
            p.pEthnicityID,
            p.pLanguageID,
            p.pSmokingStatusID,
            p.pSmokingStartDate,
            p.pSmokingEndDate,
            p.pMedicalAlert,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pAddress1))           AS pAddress1,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pAddress2))           AS pAddress2,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pCity))               AS pCity,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pState))              AS pState,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pZip))                AS pZip,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pPhone))              AS pPhone,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pCellPhone))          AS pCellPhone,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmail))              AS pEmail,
            p.pPreferedContactMethodID,
            p.pOccupation,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerName))       AS pEmployerName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerAddress1))   AS pEmployerAddress1,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerAddress2))   AS pEmployerAddress2,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerCity))       AS pEmployerCity,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerState))      AS pEmployerState,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerZip))        AS pEmployerZip,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pEmployerPhone))      AS pEmployerPhone,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianSSN))  AS pParentGuardianSSN,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianFirstName))  AS pParentGuardianFirstName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianLastName))   AS pParentGuardianLastName,
            p.pParentGuardianMiddleInitial,
            TRY_CONVERT(date, CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianDOB))) AS pParentGuardianDOB,
            p.pParentGuardianSex,
            p.pParentGuardianMaritalStatus,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianAddress1))   AS pParentGuardianAddress1,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianAddress2))   AS pParentGuardianAddress2,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianCity))       AS pParentGuardianCity,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianState))      AS pParentGuardianState,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianZip))        AS pParentGuardianZip,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianPhoneNumber)) AS pParentGuardianPhoneNumber,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianCellPhoneNumber)) AS pParentGuardianCellPhoneNumber,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerName))   AS pParentGuardianEmployerName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerAddress1)) AS pParentGuardianEmployerAddress1,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerAddress2)) AS pParentGuardianEmployerAddress2,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerCity))   AS pParentGuardianEmployerCity,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerState))  AS pParentGuardianEmployerState,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pParentGuardianEmployerZip))    AS pParentGuardianEmployerZip,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pRelativeFirstName))  AS pRelativeFirstName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pRelativeLastName))   AS pRelativeLastName,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pRelativePhone))      AS pRelativePhone,
            p.pRelativeRelation,
            p.pRelativeRelationRoleCode,
            CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pInsuredFullName))    AS pInsuredFullName,
            TRY_CONVERT(date, CONVERT(NVARCHAR(256), DECRYPTBYKEY(p.pInsuredDOB))) AS pInsuredDOB,
            p.pInsuredEmployerName,
            p.pReferralTypeID,
            p.pNoProblems,
            p.pNoMedications,
            p.pNoAllergies,
            p.pReceiveEmailReminders
        FROM dbo.Patients p
        WHERE p.pMergedToPatientID IS NULL OR p.pMergedToPatientID = '0' OR p.pMergedToPatientID = ''
        ORDER BY p.pUniqueID
        OFFSET {offset} ROWS FETCH NEXT {batchSize} ROWS ONLY
        """;

    public static CreatePatientCommand Map(SqlDataReader r)
    {
        ArgumentNullException.ThrowIfNull(r);

        var pId = GetStringOrEmpty(r, "pID");

        return new CreatePatientCommand(
            PatientCode: $"P-{pId}",
            IsActive: GetBoolOrDefault(r, "pIsActive", true),
            FirstName: GetStringOrEmpty(r, "pFirstName"),
            LastName: GetStringOrEmpty(r, "pLastName"),
            MiddleInitial: GetString(r, "pMI"),
            DateOfBirth: GetDateOrDefault(r, "pDOB"),
            Gender: GetStringOrEmpty(r, "pSex"),
            MaritalStatus: GetString(r, "pMaritalStatus"),
            IsMinor: GetBoolOrDefault(r, "pMinor", false),
            RaceId: GetInt(r, "pRaceID"),
            EthnicityId: GetInt(r, "pEthnicityID"),
            LanguageId: GetInt(r, "pLanguageID"),
            SmokingStatusId: GetInt(r, "pSmokingStatusID"),
            SmokingStartDate: GetDate(r, "pSmokingStartDate"),
            SmokingEndDate: GetDate(r, "pSmokingEndDate"),
            MedicalAlertNotes: GetString(r, "pMedicalAlert"),
            Address1: GetString(r, "pAddress1"),
            Address2: GetString(r, "pAddress2"),
            City: GetString(r, "pCity"),
            State: GetString(r, "pState"),
            ZipCode: GetString(r, "pZip"),
            Phone: GetString(r, "pPhone"),
            PhoneExtension: null,           // not in BronstonChiro Patients table
            CellPhone: GetString(r, "pCellPhone"),
            Email: GetString(r, "pEmail"),
            PreferredContactMethodId: GetInt(r, "pPreferedContactMethodID"),
            Ssn: GetString(r, "pSSN"),
            GuardianSsn: GetString(r, "pParentGuardianSSN"),
            Occupation: GetString(r, "pOccupation"),
            EmployerName: GetString(r, "pEmployerName"),
            EmployerAddress1: GetString(r, "pEmployerAddress1"),
            EmployerAddress2: GetString(r, "pEmployerAddress2"),
            EmployerCity: GetString(r, "pEmployerCity"),
            EmployerState: GetString(r, "pEmployerState"),
            EmployerZipCode: GetString(r, "pEmployerZip"),
            EmployerPhone: GetString(r, "pEmployerPhone"),
            EmployerPhoneExtension: null,   // not in BronstonChiro Patients table
            GuardianFirstName: GetString(r, "pParentGuardianFirstName"),
            GuardianLastName: GetString(r, "pParentGuardianLastName"),
            GuardianMiddleInitial: GetString(r, "pParentGuardianMiddleInitial"),
            GuardianDateOfBirth: GetDate(r, "pParentGuardianDOB"),
            GuardianGender: GetString(r, "pParentGuardianSex"),
            GuardianMaritalStatus: GetString(r, "pParentGuardianMaritalStatus"),
            GuardianAddress1: GetString(r, "pParentGuardianAddress1"),
            GuardianAddress2: GetString(r, "pParentGuardianAddress2"),
            GuardianCity: GetString(r, "pParentGuardianCity"),
            GuardianState: GetString(r, "pParentGuardianState"),
            GuardianZipCode: GetString(r, "pParentGuardianZip"),
            GuardianPhone: GetString(r, "pParentGuardianPhoneNumber"),
            GuardianCellPhone: GetString(r, "pParentGuardianCellPhoneNumber"),
            GuardianEmployerName: GetString(r, "pParentGuardianEmployerName"),
            GuardianEmployerAddress1: GetString(r, "pParentGuardianEmployerAddress1"),
            GuardianEmployerAddress2: GetString(r, "pParentGuardianEmployerAddress2"),
            GuardianEmployerCity: GetString(r, "pParentGuardianEmployerCity"),
            GuardianEmployerState: GetString(r, "pParentGuardianEmployerState"),
            GuardianEmployerZipCode: GetString(r, "pParentGuardianEmployerZip"),
            NextOfKinFirstName: GetString(r, "pRelativeFirstName"),
            NextOfKinLastName: GetString(r, "pRelativeLastName"),
            NextOfKinPhone: GetString(r, "pRelativePhone"),
            NextOfKinRelation: GetString(r, "pRelativeRelation"),
            NextOfKinRelationRoleCode: GetString(r, "pRelativeRelationRoleCode"),
            InsuredFullName: GetString(r, "pInsuredFullName"),
            InsuredDateOfBirth: GetDate(r, "pInsuredDOB"),
            InsuredEmployerName: GetString(r, "pInsuredEmployerName"),
            ReferralTypeId: GetInt(r, "pReferralTypeID"),
            HasNoKnownProblems: GetBoolOrDefault(r, "pNoProblems", false),
            HasNoKnownMedications: GetBoolOrDefault(r, "pNoMedications", false),
            HasNoKnownAllergies: GetBoolOrDefault(r, "pNoAllergies", false),
            ReceivesEmailReminders: GetBoolOrDefault(r, "pReceiveEmailReminders", false));
    }

    private static string GetStringOrEmpty(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? string.Empty : r.GetString(ord).Trim();
    }

    private static string? GetString(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return null;
        var val = r.GetString(ord).Trim();
        return val.Length == 0 ? null : val;
    }

    private static DateTime GetDateOrDefault(SqlDataReader r, string col)
    {
        var ord = r.GetOrdinal(col);
        if (r.IsDBNull(ord)) return DateTime.Today;
        var dt = r.GetDateTime(ord);
        return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);
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
        return r.IsDBNull(ord) ? null : Convert.ToInt32(r.GetValue(ord), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool GetBoolOrDefault(SqlDataReader r, string col, bool defaultValue)
    {
        var ord = r.GetOrdinal(col);
        return r.IsDBNull(ord) ? defaultValue : r.GetBoolean(ord);
    }
}
