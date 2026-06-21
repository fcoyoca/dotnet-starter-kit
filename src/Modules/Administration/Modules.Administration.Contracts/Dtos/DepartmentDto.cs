namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
