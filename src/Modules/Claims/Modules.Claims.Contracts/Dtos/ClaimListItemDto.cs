namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimListItemDto(
    Guid Id,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    Guid? InsuranceTypeId,
    ClaimStatus Status,
    decimal TotalCharge,
    int LineCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ResolvedAtUtc);
