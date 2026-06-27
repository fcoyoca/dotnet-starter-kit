using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Services;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.GetPatientReportById;

public sealed class GetPatientReportByIdQueryHandler(PatientDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetPatientReportByIdQuery, PatientReportDetailDto>
{
    public async ValueTask<PatientReportDetailDto> Handle(
        GetPatientReportByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Domain.PatientReport report = await dbContext.PatientReports
            .Include(x => x.FieldValues)
            .Include(x => x.Addendums)
            .Include(x => x.AssociatedProblems)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {query.ReportId} not found.");

        return new PatientReportDetailDto(
            report.Id, report.IncidentId, report.PatientId, report.ReportTypeId, report.ReportDate,
            report.Version, report.ProviderId, report.ClinicId, report.IsNoShow,
            new ReportVitalsDto(
                report.Vitals.HeightInches, report.Vitals.WeightLbs, report.Vitals.Bmi,
                report.Vitals.Systolic, report.Vitals.Diastolic, report.Vitals.Pulse, report.Vitals.TemperatureF),
            report.WorkflowStatus, report.IsSigned,
            report.SignedByUserId, report.SignedByName, report.SignedOnUtc,
            report.SignatureImagePath, ReportSignatureUrl.Resolve(report.SignatureImagePath, httpContextAccessor),
            report.ReviewRequestedByUserId, report.ReviewRequestedOnUtc, report.ReviewerProviderId,
            report.ReviewSignedByUserId, report.ReviewSignedByName, report.ReviewSignedOnUtc,
            report.ReviewSignatureImagePath, ReportSignatureUrl.Resolve(report.ReviewSignatureImagePath, httpContextAccessor),
            report.FieldValues.Select(f => new ReportFieldValueDto(f.ReportFieldId, f.Text)).ToList(),
            report.Addendums
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new ReportAddendumDto(a.Id, a.Text, a.CreatedByUserId, a.CreatedByName, a.CreatedAtUtc))
                .ToList(),
            report.AssociatedProblems.Select(p => p.ProblemId).ToList(),
            report.CreatedAtUtc, report.UpdatedAtUtc);
    }
}
