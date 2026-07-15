namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimsSummaryDto(
    int Draft,
    int Ready,
    int Submitted,
    int Paid,
    int Denied,
    decimal OutstandingCharge);
