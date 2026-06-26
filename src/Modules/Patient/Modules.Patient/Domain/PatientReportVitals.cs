namespace FSH.Modules.Patient.Domain;

/// <summary>Owned value object: visit vitals. All nullable; BMI is computed client-side.</summary>
public sealed class PatientReportVitals
{
    public decimal? HeightInches { get; private set; }
    public decimal? WeightLbs { get; private set; }
    public decimal? Bmi { get; private set; }
    public int? Systolic { get; private set; }
    public int? Diastolic { get; private set; }
    public int? Pulse { get; private set; }
    public decimal? TemperatureF { get; private set; }

    public PatientReportVitals() { }

    public PatientReportVitals(
        decimal? heightInches, decimal? weightLbs, decimal? bmi,
        int? systolic, int? diastolic, int? pulse, decimal? temperatureF)
    {
        HeightInches = heightInches;
        WeightLbs = weightLbs;
        Bmi = bmi;
        Systolic = systolic;
        Diastolic = diastolic;
        Pulse = pulse;
        TemperatureF = temperatureF;
    }
}
