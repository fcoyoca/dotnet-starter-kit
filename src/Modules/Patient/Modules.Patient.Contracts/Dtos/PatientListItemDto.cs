namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientListItemDto(
    Guid Id,
    string PatientCode,
    string FirstName,
    string LastName,
    string? MiddleInitial,
    DateTime DateOfBirth,
    string Gender,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime? LastVisitDate,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
