using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record DeleteReportFieldCommand(int Id) : ICommand<Unit>;
