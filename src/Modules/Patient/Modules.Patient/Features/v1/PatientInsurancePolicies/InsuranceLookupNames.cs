using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies;

/// <summary>
/// Resolves payer and plan-type display names for the insurance-policy read handlers. Names live in
/// the Administration module, so they are pulled through its Contracts (cross-module read via
/// <see cref="IMediator"/>) rather than joined — EF never crosses a module boundary here.
///
/// Names are resolved on read, not snapshotted onto the policy row, so an Administration rename
/// flows through to existing policies. The trade-off is the page cap below: a tenant with more
/// payers than <see cref="LookupPageSize"/> will see nulls for the overflow, and the dashboard
/// falls back to its own lookup options. Revisit if any tenant approaches that many payers.
/// </summary>
internal static class InsuranceLookupNames
{
    private const int LookupPageSize = 200;

    internal static async Task<(Dictionary<Guid, string> Companies, Dictionary<Guid, string> Types)> ResolveAsync(
        IMediator mediator, CancellationToken cancellationToken)
    {
        PagedResponse<InsuranceCompanyDto> companies = await mediator
            .Send(new ListInsuranceCompaniesQuery(PageSize: LookupPageSize), cancellationToken)
            .ConfigureAwait(false);

        PagedResponse<InsuranceTypeDto> types = await mediator
            .Send(new ListInsuranceTypesQuery(PageSize: LookupPageSize), cancellationToken)
            .ConfigureAwait(false);

        return (
            companies.Items.ToDictionary(c => c.Id, c => c.Name),
            types.Items.ToDictionary(t => t.Id, t => t.Name));
    }

    internal static string? NameOrNull(this Dictionary<Guid, string> map, Guid? id) =>
        id is not null && map.TryGetValue(id.Value, out string? name) ? name : null;

    /// <summary>Masks the subscriber's SSN — plaintext is never returned, matching the patient's own SSN.</summary>
    private static string? MaskSsn(string? ssn) =>
        string.IsNullOrEmpty(ssn) || ssn.Length < 4 ? ssn : $"***-**-{ssn[^4..]}";

    internal static PatientInsurancePolicyDto ToDto(
        this Domain.PatientInsurancePolicy x, string? companyName, string? typeName) =>
        new(
            x.Id,
            x.PatientId,
            x.InsuranceCompanyId,
            companyName,
            x.InsuranceTypeId,
            typeName,
            x.Priority,
            x.PolicyNumber,
            x.GroupNumber,
            x.MemberId,
            x.CoPay,
            x.Deductible,
            x.EffectiveDate,
            x.ExpirationDate,
            x.SubscriberRelationship,
            x.SubscriberFirstName,
            x.SubscriberLastName,
            x.SubscriberDateOfBirth,
            x.SubscriberGender,
            MaskSsn(x.SubscriberSsn),
            x.SubscriberEmployerName,
            x.SubscriberAddress1,
            x.SubscriberAddress2,
            x.SubscriberCity,
            x.SubscriberState,
            x.SubscriberZipCode,
            x.Notes,
            x.IsActive,
            x.CreatedByName,
            x.CreatedAtUtc,
            x.UpdatedByName,
            x.UpdatedAtUtc);
}
