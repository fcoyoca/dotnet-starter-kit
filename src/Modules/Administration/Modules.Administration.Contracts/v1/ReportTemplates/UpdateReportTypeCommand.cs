using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReportTemplates;

public sealed record UpdateReportTypeCommand(int Id, string Name, int DisplayOrder, bool IsActive) : ICommand<Unit>;
