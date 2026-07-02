using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.NextPatientCodePreview;

public sealed class NextPatientCodePreviewQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<NextPatientCodePreviewQuery, NextPatientCodePreviewDto>
{
    public async ValueTask<NextPatientCodePreviewDto> Handle(
        NextPatientCodePreviewQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""SELECT last_value, is_called FROM "{PatientDbContext.Schema}"."PatientCodeSequence" """;
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            long lastValue = reader.GetInt64(0);
            bool isCalled = reader.GetBoolean(1);
            long next = isCalled ? lastValue + 1 : lastValue;
            return new NextPatientCodePreviewDto($"P-{next}");
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
