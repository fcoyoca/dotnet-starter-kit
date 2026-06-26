using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage;
using FSH.Framework.Storage.Services;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public sealed class SetProviderSignatureCommandHandler(AdministrationDbContext dbContext, IStorageService storage)
    : ICommandHandler<SetProviderSignatureCommand, string>
{
    public async ValueTask<string> Handle(SetProviderSignatureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.ProviderId} not found.");

        byte[] bytes = DecodePng(command.ImageBase64);

        var request = new FileUploadRequest
        {
            FileName = "signature.png",
            ContentType = "image/png",
            Data = [.. bytes]
        };

        // Stored under uploads/provider/{guid}_signature.png (path derived by UploadAsync<Provider>).
        string storedPath = await storage.UploadAsync<Domain.Provider>(request, FileType.Image, cancellationToken)
            .ConfigureAwait(false);

        entity.SetSignature(storedPath);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return storage.BuildPublicUrl(storedPath);
    }

    private static byte[] DecodePng(string imageBase64)
    {
        string payload = imageBase64;
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
            throw new CustomException("Signature image is not valid base64.", ex, HttpStatusCode.BadRequest);
        }
    }
}
