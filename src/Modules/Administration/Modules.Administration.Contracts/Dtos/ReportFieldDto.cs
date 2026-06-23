namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ReportFieldDto(
    int Id,
    int ReportTypeId,
    string Name,
    string? Category,
    int DisplayOrder,
    bool IsActive);
