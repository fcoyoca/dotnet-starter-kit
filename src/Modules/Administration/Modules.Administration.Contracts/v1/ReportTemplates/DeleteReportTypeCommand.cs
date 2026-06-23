using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record DeleteReportTypeCommand(int Id) : ICommand<Unit>;
