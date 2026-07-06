using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Patient.Services;

public sealed class PatientDocumentStorage(
    IOptions<PatientOptions> options,
    IHostEnvironment environment,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor) : IPatientDocumentStorage
{
    private const string DocumentsFolder = "patientDocuments";

    public async Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);

        // Legacy uniquifier parity: doc_{unique}{ext}. Only the original name's extension is kept —
        // the stored name is fully server-generated so no client input reaches the filesystem path.
#pragma warning disable CA1308 // extensions are intentionally lower-case on disk
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
#pragma warning restore CA1308
        string storedName = $"doc_{Guid.NewGuid():N}{extension}";
        string relativePath = Path.Combine(TenantFolder(), DocumentsFolder, storedName);

        string fullPath = Path.Combine(RootPath(), relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken).ConfigureAwait(false);

        return relativePath.Replace('\\', '/');
    }

    public async Task<byte[]?> ReadAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedPath);

        string root = RootPath();
        string fullPath = Path.GetFullPath(Path.Combine(root, storedPath.Replace('/', Path.DirectorySeparatorChar)));

        // Path-traversal guard: the resolved path must stay inside the storage root.
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Invalid document path.");
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    private string RootPath()
    {
        string root = string.IsNullOrWhiteSpace(options.Value.DocumentsRootPath)
            ? Path.Combine(environment.ContentRootPath, "secure-uploads")
            : options.Value.DocumentsRootPath;
        return Path.GetFullPath(root);
    }

    private string TenantFolder()
    {
        string tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new UnauthorizedException("Tenant context is required.");
        return $"c_{tenantId}";
    }
}
