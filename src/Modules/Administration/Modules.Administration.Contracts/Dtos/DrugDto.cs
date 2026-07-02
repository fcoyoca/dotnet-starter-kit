namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record DrugDto(
    int Id,
    string Name,
    string? RxAui,
    string? RxCui,
    string? Tty,
    string? Sab,
    string? Code,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
