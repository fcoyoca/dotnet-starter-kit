namespace FSH.Modules.Patient.Domain;

public sealed class PatientNextOfKin
{
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Phone { get; private set; }
    public string? Relation { get; private set; }
    public string? RelationRoleCode { get; private set; }

    private PatientNextOfKin() { }

    internal static PatientNextOfKin Create(
        string? firstName, string? lastName, string? phone,
        string? relation, string? relationRoleCode) =>
        new()
        {
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim(),
            Phone = phone?.Trim(),
            Relation = relation?.Trim(),
            RelationRoleCode = relationRoleCode?.Trim()
        };

    internal void Update(
        string? firstName, string? lastName, string? phone,
        string? relation, string? relationRoleCode)
    {
        FirstName = firstName?.Trim();
        LastName = lastName?.Trim();
        Phone = phone?.Trim();
        Relation = relation?.Trim();
        RelationRoleCode = relationRoleCode?.Trim();
    }
}
