namespace FSH.Modules.Patient.Services;

/// <summary>
/// Stores patient document files on local disk under a per-tenant folder —
/// <c>{root}/c_{tenantId}/patientDocuments/doc_{guid}{ext}</c> — mirroring the legacy BackChart
/// layout (<c>UploadDirectoryFullPath\c_{clientID}\patientDocuments\doc_…</c>). Interim solution:
/// isolated behind this interface so it can be swapped for the Files module / S3 later without
/// touching callers. Files live OUTSIDE wwwroot; downloads go through the permission-gated endpoint.
/// </summary>
public interface IPatientDocumentStorage
{
    /// <summary>Writes the file and returns its stored path relative to the storage root
    /// (forward-slash normalized, e.g. <c>c_root/patientDocuments/doc_….pdf</c>).</summary>
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Reads a previously stored file; null when it no longer exists on disk.</summary>
    Task<byte[]?> ReadAsync(string storedPath, CancellationToken cancellationToken = default);
}
