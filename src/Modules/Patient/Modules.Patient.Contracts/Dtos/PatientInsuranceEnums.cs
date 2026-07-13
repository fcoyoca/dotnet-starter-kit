namespace FSH.Modules.Patient.Contracts.Dtos;

/// <summary>
/// Coordination-of-benefits order for a patient's insurance policies. Replaces legacy BackChart's
/// free-form 0–9 "Priority Number" dropdown (<c>pinPriority</c>), which allowed duplicates and had
/// no billing meaning. A patient may hold at most one <em>active</em> policy per priority.
/// </summary>
public enum InsurancePriority
{
    Primary,
    Secondary,
    Tertiary,
    Quaternary
}

/// <summary>
/// The subscriber's (policy holder's) relationship to the patient — legacy
/// <c>pinRelationshipToInsured</c> (1=Self, 2=Spouse, 3=Child). When <see cref="Self"/>, the
/// subscriber block is copied from the patient's own demographics and is not separately editable.
/// </summary>
public enum SubscriberRelationship
{
    Self,
    Spouse,
    Child,
    Other
}
