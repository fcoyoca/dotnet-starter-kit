using FSH.Framework.Core.Exceptions;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.DownloadPatientDocument;

public sealed class DownloadPatientDocumentQueryHandler(
    PatientDbContext dbContext,
    IPatientDocumentStorage storage,
    IAuditPublisher auditPublisher)
    : IQueryHandler<DownloadPatientDocumentQuery, PatientDocumentFileDto>
{
    public async ValueTask<PatientDocumentFileDto> Handle(
        DownloadPatientDocumentQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Domain.PatientDocument document = await dbContext.PatientDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.DocumentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Document {query.DocumentId} not found.");

        byte[]? content = await storage.ReadAsync(document.StoredPath, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"The file for document {query.DocumentId} no longer exists on disk.");

        // HIPAA audit: downloading a patient document is a PHI disclosure (legacy BackChart
        // audit-logged every document download).
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
                Name: $"PHI_EXPORT:PatientDocument:{document.Id}",
                StatusCode: 200,
                DurationMs: 0,
                Captured: BodyCapture.None,
                RequestSize: 0,
                ResponseSize: content.Length,
                RequestPreview: null,
                ResponsePreview: null)),
            cancellationToken)
            .ConfigureAwait(false);

        return new PatientDocumentFileDto(content, document.FileName, document.ContentType);
    }
}
