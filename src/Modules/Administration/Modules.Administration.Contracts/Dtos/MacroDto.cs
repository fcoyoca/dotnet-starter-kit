namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record MacroDto(
    Guid Id,
    string Name,
    string? Text,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
