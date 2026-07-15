using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public sealed class SetReportProceduresCommandHandler(
    PatientDbContext dbContext,
    IEventBus eventBus,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<SetReportProceduresCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetReportProceduresCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        SuperBill? bill = await dbContext.SuperBills
            .Include(x => x.Procedures)
            .ThenInclude(p => p.Diagnostics)
            .FirstOrDefaultAsync(x => x.ReportId == command.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (bill is null)
        {
            bill = SuperBill.Create(command.ReportId, report.PatientId, command.InsuranceTypeId);
            dbContext.SuperBills.Add(bill);
        }
        else
        {
            bill.SetInsuranceType(command.InsuranceTypeId);
        }

        bill.ReplaceProcedures(command.Procedures ?? []);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Pluggable billing seam: provider modules (chosen per tenant) subscribe to this event —
        // published via IEventBus like Billing/Chat/Files (IOutboxStore is Identity-owned today).
        await eventBus.PublishAsync(new SuperBillSavedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: timeProvider.GetUtcNow().UtcDateTime,
                TenantId: tenantAccessor.MultiTenantContext.TenantInfo?.Id,
                CorrelationId: Guid.NewGuid().ToString(),
                Source: "Patient",
                SuperBillId: bill.Id,
                ReportId: bill.ReportId,
                PatientId: bill.PatientId,
                IsBilled: bill.IsBilled,
                Procedures: command.Procedures ?? [],
                InsuranceTypeId: bill.InsuranceTypeId), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
