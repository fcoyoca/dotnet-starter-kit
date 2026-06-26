using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientReports;

public sealed record RequestReportReviewCommand(Guid ReportId, Guid ReviewerProviderId) : ICommand<Unit>;
