namespace FSH.Modules.Administration.Contracts.Dtos;

/// <summary>One diagnostic (ICD) code associated with a diagnostic category, joined for its code/description.</summary>
public sealed record DiagnosticCategoryCodeDto(
    Guid DiagnosticCategoryId,
    int DiagnosticId,
    string Code,
    string? Description);
