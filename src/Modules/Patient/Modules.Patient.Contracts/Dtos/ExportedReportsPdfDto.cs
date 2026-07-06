namespace FSH.Modules.Patient.Contracts.Dtos;

/// <summary>A rendered patient-report PDF export. <paramref name="Sha256"/> is the lowercase hex
/// SHA-256 of <paramref name="Content"/> so clients can verify download integrity (legacy
/// BackChart "Secure Download" parity).</summary>
public sealed record ExportedReportsPdfDto(byte[] Content, string FileName, string Sha256);
