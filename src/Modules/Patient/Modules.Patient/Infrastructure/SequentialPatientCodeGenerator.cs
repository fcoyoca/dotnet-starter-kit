using System.Globalization;
using FSH.Modules.Patient.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class SequentialPatientCodeGenerator(PatientDbContext dbContext) : IPatientCodeGenerator
{
    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""SELECT nextval('"{PatientDbContext.Schema}"."PatientCodeSequence"')""";
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            long next = Convert.ToInt64(result, CultureInfo.InvariantCulture);
            return $"P-{next}";
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
