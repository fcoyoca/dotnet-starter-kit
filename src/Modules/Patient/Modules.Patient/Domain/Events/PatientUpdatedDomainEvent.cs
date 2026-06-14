using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain.Events;

public sealed record PatientUpdatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PatientId,
    string PatientCode) : DomainEvent(EventId, OccurredOnUtc);
