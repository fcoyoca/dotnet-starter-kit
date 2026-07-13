using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record UpdateReportFieldCommand(
    int Id,
    string Name,
    string? Category,
    int DisplayOrder,
    bool IsActive,
    string? DefaultText = null) : ICommand<Unit>;
