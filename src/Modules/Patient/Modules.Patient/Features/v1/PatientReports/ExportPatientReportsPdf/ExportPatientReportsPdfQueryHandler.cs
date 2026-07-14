using System.Globalization;
using System.Security.Cryptography;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Contracts.v1.Providers;
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
    IPdfPasswordProtector passwordProtector,
    IPatientDocumentStorage storage,
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

        if (reports.Select(r => r.IncidentId).Distinct().Count() > 1)
        {
            throw new CustomException("All reports in one export must belong to the same incident.");
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

        // The legacy header is incident-scoped: DOIV, DOL and the DX list. All reports in an export
        // belong to one patient; take the incident of the first report.
        Domain.PatientIncident? incident = await dbContext.PatientIncidents
            .Include(i => i.Diagnostics)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == reports[0].IncidentId && !i.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        string? diagnosisCodes = null;
        if (incident is not null && incident.Diagnostics.Count > 0)
        {
            // Incident diagnostics reference CustomDiagnostic rows (Guid keys) in Administration.
            List<Guid> diagnosticIds = incident.Diagnostics.Select(d => d.DiagnosticId).ToList();
            PagedResponse<CustomDiagnosticDto> dx = await mediator
                .Send(new ListCustomDiagnosticsQuery(Ids: diagnosticIds, PageSize: 200), cancellationToken)
                .ConfigureAwait(false);
            diagnosisCodes = dx.Items.Count == 0
                ? null
                : string.Join(", ", dx.Items.Select(d => d.Code));
        }

        // A clinic/provider/department id on an old report can point at a row that's since been
        // soft-deleted (closed clinic, offboarded provider, retired department) — a routine
        // lifecycle event, not a data-integrity error. Mirror the signature-read policy: a
        // reference that no longer resolves degrades to a missing name rather than failing the
        // whole export.
        var clinicNames = new Dictionary<Guid, string>();
        var orientations = new Dictionary<Guid, PrintOrientation>();
        foreach (Guid clinicId in reports.Where(r => r.ClinicId is not null)
                     .Select(r => r.ClinicId!.Value).Distinct())
        {
            try
            {
                ClinicDto clinic = await mediator
                    .Send(new GetClinicByIdQuery(clinicId), cancellationToken).ConfigureAwait(false);
                clinicNames[clinicId] = clinic.Name;
                orientations[clinicId] = clinic.PrintOrientation;
            }
            catch (NotFoundException)
            {
                // Clinic no longer exists — the affected report(s) fall back to no clinic name
                // and portrait orientation, same as a report with no ClinicId at all.
            }
        }

        var providerNames = new Dictionary<Guid, string>();
        foreach (Guid providerId in reports.Where(r => r.ProviderId is not null)
                     .Select(r => r.ProviderId!.Value).Distinct())
        {
            try
            {
                ProviderDto provider = await mediator
                    .Send(new GetProviderByIdQuery(providerId), cancellationToken).ConfigureAwait(false);
                providerNames[providerId] = string.Join(" ", new[] { provider.Prefix, provider.FirstName, provider.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            catch (NotFoundException)
            {
                // Provider no longer exists — the affected report(s) render without a provider name.
            }
        }

        string? departmentName = null;
        if (incident?.DepartmentId is { } departmentId)
        {
            try
            {
                DepartmentDto department = await mediator
                    .Send(new GetDepartmentByIdQuery(departmentId), cancellationToken).ConfigureAwait(false);
                departmentName = department.Name;
            }
            catch (NotFoundException)
            {
                // Department no longer exists — the header renders without a department name.
            }
        }

        // Legacy grid ordered exports by report date ascending.
        var models = new List<ReportPdfModel>();
        foreach (Domain.PatientReport report in reports
                     .OrderBy(r => r.ReportDate)
                     .ThenBy(r => r.CreatedAtUtc))
        {
            byte[]? signature = await ReadSignature(report.SignatureImagePath, cancellationToken)
                .ConfigureAwait(false);
            byte[]? reviewSignature = await ReadSignature(report.ReviewSignatureImagePath, cancellationToken)
                .ConfigureAwait(false);

            models.Add(BuildModel(
                report,
                typeNames,
                fieldsByType,
                report.ClinicId is { } cid && clinicNames.TryGetValue(cid, out string? cname) ? cname : null,
                report.ClinicId is { } oid && orientations.TryGetValue(oid, out PrintOrientation o)
                    ? o
                    : PrintOrientation.Portrait,
                departmentName,
                report.ProviderId is { } pid && providerNames.TryGetValue(pid, out string? pname) ? pname : null,
                signature,
                reviewSignature));
        }

        var patientInfo = new ReportPdfPatientInfo(
            string.Join(" ", new[]
            {
                patient.Demographics.FirstName,
                patient.Demographics.MiddleInitial,
                patient.Demographics.LastName,
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            patient.PatientCode,
            patient.Demographics.DateOfBirth,
            incident?.DateOfInitialVisit,
            incident?.DateOfLoss,
            diagnosisCodes);

        byte[] content = renderer.Render(patientInfo, models);

        bool isProtected = !string.IsNullOrWhiteSpace(query.Password);
        if (isProtected)
        {
            content = passwordProtector.Protect(content, query.Password!);
        }

        // Hash the bytes the client actually receives, so the checksum it displays matches the
        // file on disk — for a protected export that means hashing *after* encryption.
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
                Name: $"PHI_EXPORT:PatientReports{(isProtected ? ":Protected" : string.Empty)}:{string.Join(",", ids)}",
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

    private async Task<byte[]?> ReadSignature(string? storedPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return await storage.ReadAsync(storedPath, cancellationToken).ConfigureAwait(false);
    }

    private static ReportPdfModel BuildModel(
        Domain.PatientReport report,
        Dictionary<int, string> typeNames,
        Dictionary<int, IReadOnlyList<ReportFieldDto>> fieldsByType,
        string? clinicName,
        PrintOrientation orientation,
        string? departmentName,
        string? providerName,
        byte[]? signatureImage,
        byte[]? reviewSignatureImage)
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
            report.IsSigned,
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
                .ToList(),
            clinicName,
            orientation,
            departmentName,
            providerName,
            report.UpdatedAtUtc,
            signatureImage,
            reviewSignatureImage);
    }
}
