namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimLineDto(
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    IReadOnlyList<Guid> DiagnosticIds);
