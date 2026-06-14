using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Patient.Data;

public sealed class PatientDbInitializer(
    PatientDbContext dbContext,
    ILogger<PatientDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Patient] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
