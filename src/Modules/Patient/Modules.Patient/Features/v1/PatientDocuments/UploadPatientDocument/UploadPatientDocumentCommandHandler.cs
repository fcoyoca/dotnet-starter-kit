using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Services;
using Mediator;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UploadPatientDocument;

public sealed class UploadPatientDocumentCommandHandler(
    PatientDbContext dbContext,
    IPatientDocumentStorage storage,
    ICurrentUser currentUser)
    : ICommandHandler<UploadPatientDocumentCommand, Guid>
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public async ValueTask<Guid> Handle(UploadPatientDocumentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool patientExists = await dbContext.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false);
        if (!patientExists)
        {
            throw new NotFoundException($"Patient {command.PatientId} not found.");
        }

        byte[] content = DecodeContent(command.ContentBase64);

        // Strip any client-supplied directory segments; only the bare name is stored.
        string fileName = Path.GetFileName(command.FileName.Trim());
        string contentType = ResolveContentType(command.ContentType, fileName);

        string storedPath = await storage.SaveAsync(fileName, content, cancellationToken).ConfigureAwait(false);

        var document = Domain.PatientDocument.Create(
            command.PatientId,
            command.DocumentTypeId,
            fileName,
            storedPath,
            contentType,
            content.LongLength,
            command.Notes,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return document.Id;
    }

    private static byte[] DecodeContent(string contentBase64)
    {
        string payload = contentBase64;
        int comma = payload.IndexOf(',', StringComparison.Ordinal);
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
        {
            payload = payload[(comma + 1)..];
        }

        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException ex)
        {
            throw new CustomException("Document content is not valid base64.", ex, HttpStatusCode.BadRequest);
        }
    }

    private static string ResolveContentType(string? provided, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(provided))
        {
            return provided;
        }

        return ContentTypeProvider.TryGetContentType(fileName, out string? detected)
            ? detected
            : "application/octet-stream";
    }
}
