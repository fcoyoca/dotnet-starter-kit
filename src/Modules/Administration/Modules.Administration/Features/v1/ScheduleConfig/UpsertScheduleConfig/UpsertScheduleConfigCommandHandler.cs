using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ScheduleConfig;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ScheduleConfig.UpsertScheduleConfig;

public sealed class UpsertScheduleConfigCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpsertScheduleConfigCommand, ScheduleConfigDto>
{
    public async ValueTask<ScheduleConfigDto> Handle(UpsertScheduleConfigCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool clinicExists = await dbContext.Clinics
            .AnyAsync(c => c.Id == command.ClinicId, cancellationToken)
            .ConfigureAwait(false);
        if (!clinicExists)
        {
            throw new NotFoundException($"Clinic {command.ClinicId} not found.");
        }

        Domain.ScheduleConfig? entity = await dbContext.ScheduleConfigs
            .FirstOrDefaultAsync(s => s.ClinicId == command.ClinicId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = Domain.ScheduleConfig.Create(
                command.ClinicId, command.StartTime, command.EndTime, command.IntervalMinutes);
            await dbContext.ScheduleConfigs.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            entity.Update(command.StartTime, command.EndTime, command.IntervalMinutes);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ScheduleConfigDto(entity.ClinicId, entity.StartTime, entity.EndTime, entity.IntervalMinutes);
    }
}
