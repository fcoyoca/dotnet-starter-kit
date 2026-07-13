using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record CreateReportFieldCommand(
    int ReportTypeId,
    string Name,
    string? Category = null,
    int DisplayOrder = 0,
    string? DefaultText = null) : ICommand<int>;
