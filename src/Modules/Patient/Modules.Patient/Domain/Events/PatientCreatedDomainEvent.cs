using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain.Events;

public sealed record PatientCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PatientId,
    string PatientCode) : DomainEvent(EventId, OccurredOnUtc);
