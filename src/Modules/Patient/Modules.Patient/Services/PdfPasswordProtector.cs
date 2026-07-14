using System.Security.Cryptography;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;

namespace FSH.Modules.Patient.Services;

/// <summary>
/// PDFsharp-based AES-256 encryption of a rendered report PDF. QuestPDF can also encrypt, but only
/// via file paths (<c>DocumentOperation.LoadFile</c>/<c>Save</c>) — that would mean writing the
/// *unencrypted* PHI document to disk in order to produce the encrypted one, defeating the purpose.
/// PDFsharp works on streams, so the plaintext PDF never leaves memory.
/// </summary>
public sealed class PdfPasswordProtector : IPdfPasswordProtector
{
    public byte[] Protect(byte[] pdf, string userPassword)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPassword);

        using var input = new MemoryStream(pdf, writable: false);
        using PdfDocument document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);

        document.SecurityHandler.SetEncryption(PdfDefaultEncryption.V5);

        PdfSecuritySettings security = document.SecuritySettings;
        security.UserPassword = userPassword;

        // A random owner password nobody keeps: the owner password is what grants permission to
        // strip the restrictions below, and an empty one (or one equal to the user password) would
        // hand that power to every recipient. Discarding it means the permissions actually hold.
        security.OwnerPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // The recipient is meant to read and print the record, not repackage it.
        security.PermitPrint = true;
        security.PermitFullQualityPrint = true;
        security.PermitExtractContent = false;
        security.PermitModifyDocument = false;
        security.PermitAssembleDocument = false;
        security.PermitAnnotations = false;
        security.PermitFormsFill = false;

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }
}
