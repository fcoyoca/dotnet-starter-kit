using FSH.Framework.Core.Context;
using FSH.Framework.Web.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace FSH.Modules.Scheduling.Features.v1.Appointments;

/// <summary>
/// Broadcasts appointment changes to the tenant's SignalR group so every open scheduler refreshes live —
/// replacing the legacy 60-second polling timer. Best-effort: a missing tenant id is a no-op.
/// </summary>
public sealed class AppointmentRealtimeNotifier(IHubContext<AppHub> hub, ICurrentUser currentUser)
{
    public const string EventName = "AppointmentChanged";

    public async Task NotifyChangedAsync(
        Guid clinicId, Guid providerId, DateTime startUtc, DateTime endUtc, string action, CancellationToken ct)
    {
        string? tenantId = currentUser.GetTenant();
        if (string.IsNullOrEmpty(tenantId))
        {
            return;
        }

        await hub.Clients.Group($"tenant:{tenantId}")
            .SendAsync(EventName, new { clinicId, providerId, startUtc, endUtc, action }, ct)
            .ConfigureAwait(false);
    }
}
