using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Domain;
using ContractsClaimStatus = FSH.Modules.Claims.Contracts.ClaimStatus;

namespace FSH.Modules.Claims.Features.v1.Claims;

internal static class ClaimMappings
{
    public static ClaimDetailDto ToDetailDto(this Claim c) => new(
        c.Id, c.SuperBillId, c.ReportId, c.PatientId, c.InsuranceTypeId,
        (ContractsClaimStatus)(int)c.Status, c.TotalCharge, c.ControlNumber,
        c.CreatedAtUtc, c.UpdatedAtUtc, c.SubmittedAtUtc, c.ResolvedAtUtc,
        c.Lines.Select(l => new ClaimLineDto(l.ProcedureCodeId, l.Code, l.Description, l.Charge, l.DiagnosticIds)).ToList());

    public static ClaimListItemDto ToListItemDto(this Claim c) => new(
        c.Id, c.SuperBillId, c.ReportId, c.PatientId, c.InsuranceTypeId,
        (ContractsClaimStatus)(int)c.Status, c.TotalCharge, c.Lines.Count,
        c.CreatedAtUtc, c.SubmittedAtUtc, c.ResolvedAtUtc);
}
