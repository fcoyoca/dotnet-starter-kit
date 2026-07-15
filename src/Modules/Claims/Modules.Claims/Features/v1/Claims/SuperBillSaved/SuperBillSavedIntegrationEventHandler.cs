using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Claims.Features.v1.Claims.SuperBillSaved;

/// <summary>
/// Upserts a claim from a saved super bill, keyed by SuperBillId. New → create Draft. Existing &amp;
/// still Draft → refresh the snapshot. Existing &amp; past Draft → skip (freeze so a biller's work
/// isn't clobbered by a late chart edit). Idempotent on redelivery.
/// </summary>
/// <remarks>
/// Tenant context is NOT set here: the event pipeline installs it centrally. <c>InMemoryEventBus</c>
/// opens <c>IEventTenantScope.Begin(@event.TenantId)</c> (Finbuckle-backed) BEFORE it constructs this
/// handler and its <see cref="ClaimsDbContext"/>, and <c>MultiTenantDbContext</c> captures its
/// <c>TenantInfo</c> at construction — so the DbContext is already tenant-scoped by the time this runs
/// (mirrors Billing's <c>TenantSubscribedIntegrationEventHandler</c>). A null TenantId means no scope
/// was installed, so we fail fast rather than write under an ambiguous tenant.
/// </remarks>
public sealed class SuperBillSavedIntegrationEventHandler(
    ClaimsDbContext db,
    ILogger<SuperBillSavedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<SuperBillSavedIntegrationEvent>
{
    public async Task HandleAsync(SuperBillSavedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event.TenantId is null)
        {
            throw new InvalidOperationException("SuperBillSavedIntegrationEvent is missing TenantId.");
        }

        var existing = await db.Claims
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.SuperBillId == @event.SuperBillId, ct)
            .ConfigureAwait(false);

        var lines = ToLines(@event.Procedures);

        if (existing is null)
        {
            var claim = Claim.CreateFromSuperBill(
                @event.SuperBillId, @event.ReportId, @event.PatientId,
                @event.InsuranceTypeId, @event.IsBilled, lines);
            db.Claims.Add(claim);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("[Claims] created draft claim {ClaimId} from super bill {SuperBillId}",
                    claim.Id, @event.SuperBillId);
            }
            return;
        }

        if (!existing.IsSnapshotEditable)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "[Claims] super bill {SuperBillId} re-saved but claim {ClaimId} is {Status}; snapshot frozen",
                    @event.SuperBillId, existing.Id, existing.Status);
            }
            return;
        }

        // Capture the currently-tracked lines before the domain method clears them: Claim.RefreshSnapshot
        // always rebuilds the line collection from scratch (see Claim.ReplaceLines), so every post-refresh
        // ClaimLine is a brand-new instance with a client-generated (non-store-generated) Guid key. Left to
        // implicit DetectChanges graph-fixup, EF cannot tell those apart from pre-existing rows and marks
        // them Modified instead of Added — an UPDATE against a row that was never inserted. Explicit
        // Remove/Add sidesteps that ambiguity entirely.
        var previousLines = existing.Lines.ToList();
        existing.RefreshSnapshot(@event.InsuranceTypeId, @event.IsBilled, lines);
        db.ClaimLines.RemoveRange(previousLines);
        db.ClaimLines.AddRange(existing.Lines);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Claims] refreshed draft claim {ClaimId} from super bill {SuperBillId}",
                existing.Id, @event.SuperBillId);
        }
    }

    private static IReadOnlyList<ClaimLine> ToLines(IReadOnlyList<ReportProcedureItem> procedures) =>
        (procedures ?? [])
            .Select(p => ClaimLine.Create(Guid.Empty, p.ProcedureCodeId, p.Code, p.Description, p.Charge, p.DiagnosticIds))
            .ToList();
}
