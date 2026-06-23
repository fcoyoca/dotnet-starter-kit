using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record CreateReportTypeCommand(string Name, int DisplayOrder = 0) : ICommand<int>;
