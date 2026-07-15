using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Contracts.Events;

/// <summary>
/// Raised after a report's procedures-performed set is saved. This is the pluggable billing seam:
/// third-party billing-provider modules (Kareo/Cvikota-style, chosen per tenant) subscribe with
/// <c>IIntegrationEventHandler&lt;SuperBillSavedIntegrationEvent&gt;</c> — no changes to the Patient
/// module are needed to add a provider. Keep this type's name and namespace stable.
/// </summary>
public sealed record SuperBillSavedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    bool IsBilled,
    IReadOnlyList<ReportProcedureItem> Procedures,
    Guid? InsuranceTypeId = null)
    : IIntegrationEvent;
