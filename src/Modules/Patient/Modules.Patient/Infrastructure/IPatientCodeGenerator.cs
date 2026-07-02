namespace FSH.Modules.Patient.Infrastructure;

public interface IPatientCodeGenerator
{
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken);
}
