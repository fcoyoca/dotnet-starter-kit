namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record ReportVitalsDto(
    decimal? HeightInches,
    decimal? WeightLbs,
    decimal? Bmi,
    int? Systolic,
    int? Diastolic,
    int? Pulse,
    decimal? TemperatureF);

public sealed record ReportFieldValueDto(int ReportFieldId, string Text);

public sealed record ReportAddendumDto(
    Guid Id,
    string Text,
    string CreatedByUserId,
    string? CreatedByName,
    DateTime CreatedAtUtc);
