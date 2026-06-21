using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// Associates a <see cref="ProcedureCode"/> with an <see cref="InsuranceType"/> at a negotiated
/// <see cref="Price"/> (legacy <c>InsuranceTypeProcedures</c> — <c>itpID</c>/<c>itpPrice</c>). Tenant-scoped —
/// not <see cref="IGlobalEntity"/>, so <c>BaseDbContext</c> applies the per-tenant filter. A pure join row:
/// removing the association is a hard delete (no soft-delete).
/// </summary>
public sealed class InsuranceTypeProcedure : AggregateRoot<Guid>
{
    public Guid InsuranceTypeId { get; private set; }
    public Guid ProcedureCodeId { get; private set; }

    /// <summary>Negotiated price for this procedure under this insurance type (legacy <c>itpPrice</c>).</summary>
    public decimal Price { get; private set; }

    /// <summary>Legacy <c>itpID</c> of the source record; null for native records.</summary>
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private InsuranceTypeProcedure() { }

    public static InsuranceTypeProcedure Create(
        Guid insuranceTypeId,
        Guid procedureCodeId,
        decimal price,
        int? legacyId = null)
    {
        if (insuranceTypeId == Guid.Empty)
        {
            throw new ArgumentException("Insurance type id is required.", nameof(insuranceTypeId));
        }

        if (procedureCodeId == Guid.Empty)
        {
            throw new ArgumentException("Procedure code id is required.", nameof(procedureCodeId));
        }

        return new InsuranceTypeProcedure
        {
            Id = Guid.CreateVersion7(),
            InsuranceTypeId = insuranceTypeId,
            ProcedureCodeId = procedureCodeId,
            Price = price < 0 ? 0 : price,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdatePrice(decimal price)
    {
        Price = price < 0 ? 0 : price;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
