namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimDetailDto(
    Guid Id,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    Guid? InsuranceTypeId,
    ClaimStatus Status,
    decimal TotalCharge,
    string? ControlNumber,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<ClaimLineDto> Lines);
