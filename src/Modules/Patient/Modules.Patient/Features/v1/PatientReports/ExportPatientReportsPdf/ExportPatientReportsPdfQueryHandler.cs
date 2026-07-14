using System.Globalization;
using System.Security.Cryptography;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;

public sealed class ExportPatientReportsPdfQueryHandler(
    PatientDbContext dbContext,
    IMediator mediator,
    IPatientReportPdfRenderer renderer,
    IAuditPublisher auditPublisher)
    : IQueryHandler<ExportPatientReportsPdfQuery, ExportedReportsPdfDto>
{
    public async ValueTask<ExportedReportsPdfDto> Handle(
        ExportPatientReportsPdfQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        List<Guid> ids = query.ReportIds.Distinct().ToList();

        List<Domain.PatientReport> reports = await dbContext.PatientReports
            .Include(x => x.FieldValues)
            .Include(x => x.Addendums)
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (reports.Count != ids.Count)
        {
            IEnumerable<Guid> missing = ids.Except(reports.Select(r => r.Id));
            throw new NotFoundException($"Report(s) not found: {string.Join(", ", missing)}.");
        }

        if (reports.Select(r => r.PatientId).Distinct().Count() > 1)
        {
            throw new CustomException("All reports in one export must belong to the same patient.");
        }

        Guid patientId = reports[0].PatientId;
        Domain.Patient patient = await dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {patientId} not found.");

        // Lookup names live in Administration — cross-module read via Contracts.
        IReadOnlyList<ReportTypeDto> reportTypes =
            await mediator.Send(new ListReportTypesQuery(), cancellationToken).ConfigureAwait(false);
        Dictionary<int, string> typeNames = reportTypes.ToDictionary(t => t.Id, t => t.Name);

        var fieldsByType = new Dictionary<int, IReadOnlyList<ReportFieldDto>>();
        foreach (int reportTypeId in reports.Select(r => r.ReportTypeId).Distinct())
        {
            fieldsByType[reportTypeId] = await mediator
                .Send(new ListReportFieldsQuery(reportTypeId, IncludeInactive: true), cancellationToken)
                .ConfigureAwait(false);
        }

        // Legacy grid ordered exports by report date ascending.
        List<ReportPdfModel> models = reports
            .OrderBy(r => r.ReportDate)
            .ThenBy(r => r.CreatedAtUtc)
            .Select(r => BuildModel(r, typeNames, fieldsByType))
            .ToList();

        var patientInfo = new ReportPdfPatientInfo(
            string.Join(" ", new[]
            {
                patient.Demographics.FirstName,
                patient.Demographics.MiddleInitial,
                patient.Demographics.LastName,
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            patient.PatientCode,
            patient.Demographics.DateOfBirth,
            patient.Demographics.Gender);

        byte[] content = renderer.Render(patientInfo, models);
        string sha256 = Convert.ToHexStringLower(SHA256.HashData(content));
        string fileName = reports.Count == 1
            ? $"report_{reports[0].Id:N}.pdf"
            : $"reports_{patient.PatientCode}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.pdf";

        // HIPAA audit: exporting reports is a PHI disclosure (legacy BackChart logged
        // PatientReport/Exported on every generate).
        await auditPublisher.PublishAsync(new AuditEnvelope(
            id: Guid.NewGuid(),
            occurredAtUtc: DateTime.UtcNow,
            receivedAtUtc: DateTime.UtcNow,
            eventType: AuditEventType.Activity,
            severity: AuditSeverity.Information,
            tenantId: auditPublisher.CurrentScope.TenantId,
            userId: auditPublisher.CurrentScope.UserId,
            userName: auditPublisher.CurrentScope.UserName,
            traceId: auditPublisher.CurrentScope.TraceId,
            spanId: auditPublisher.CurrentScope.SpanId,
            correlationId: auditPublisher.CurrentScope.CorrelationId,
            requestId: auditPublisher.CurrentScope.RequestId,
            source: "Patient",
            tags: AuditTag.PiiMasked,
            payload: new ActivityEventPayload(
                Kind: ActivityKind.Query,
                Name: $"PHI_EXPORT:PatientReports:{string.Join(",", ids)}",
                StatusCode: 200,
                DurationMs: 0,
                Captured: BodyCapture.None,
                RequestSize: 0,
                ResponseSize: content.Length,
                RequestPreview: null,
                ResponsePreview: null)),
            cancellationToken)
            .ConfigureAwait(false);

        return new ExportedReportsPdfDto(content, fileName, sha256);
    }

    /// <summary>Legacy report category that carries vitals (rcID 8) — a report type prints
    /// vitals only when its field template includes this category.</summary>
    private const string VitalsCategory = "Clinical Exam";

    private static ReportPdfModel BuildModel(
        Domain.PatientReport report,
        Dictionary<int, string> typeNames,
        Dictionary<int, IReadOnlyList<ReportFieldDto>> fieldsByType)
    {
        IReadOnlyList<ReportFieldDto> fields = fieldsByType[report.ReportTypeId];
        Dictionary<int, ReportFieldDto> fieldById = fields.ToDictionary(f => f.Id);

        bool supportsVitals = fields.Any(f =>
            string.Equals(f.Category?.Trim(), VitalsCategory, StringComparison.OrdinalIgnoreCase));

        // Sections follow the template's display order; values whose field definition no longer
        // exists still render (at the end) so no captured text is silently dropped.
        List<ReportPdfSection> sections = report.FieldValues
            .OrderBy(v => fieldById.TryGetValue(v.ReportFieldId, out ReportFieldDto? f) ? f.DisplayOrder : int.MaxValue)
            .ThenBy(v => v.ReportFieldId)
            .Select(v => fieldById.TryGetValue(v.ReportFieldId, out ReportFieldDto? f)
                ? new ReportPdfSection(f.Name, f.Category, v.Text)
                : new ReportPdfSection($"Field {v.ReportFieldId}", null, v.Text))
            .ToList();

        return new ReportPdfModel(
            typeNames.TryGetValue(report.ReportTypeId, out string? typeName) ? typeName : "Report",
            report.ReportDate,
            report.Version,
            report.IsNoShow,
            report.WorkflowStatus.ToString(),
            new ReportVitalsDto(
                report.Vitals.HeightInches, report.Vitals.WeightLbs, report.Vitals.Bmi,
                report.Vitals.Systolic, report.Vitals.Diastolic, report.Vitals.Pulse,
                report.Vitals.TemperatureF),
            supportsVitals,
            sections,
            report.SignedByName,
            report.SignedOnUtc,
            report.ReviewSignedByName,
            report.ReviewSignedOnUtc,
            report.Addendums
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new ReportPdfAddendum(a.CreatedByName, a.CreatedAtUtc, a.Text))
                .ToList());
    }
}
