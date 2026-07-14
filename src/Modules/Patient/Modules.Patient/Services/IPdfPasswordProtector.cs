namespace FSH.Modules.Patient.Services;

/// <summary>Applies password protection to an already-rendered PDF (legacy BackChart's padlock
/// button next to Export PDF, which produced an encrypted download). Kept separate from
/// <see cref="IPatientReportPdfRenderer"/> so the renderer stays a pure model → bytes function.</summary>
public interface IPdfPasswordProtector
{
    /// <summary>Returns <paramref name="pdf"/> re-encoded with AES-256 encryption; the document
    /// then opens only with <paramref name="userPassword"/>.</summary>
    byte[] Protect(byte[] pdf, string userPassword);
}
