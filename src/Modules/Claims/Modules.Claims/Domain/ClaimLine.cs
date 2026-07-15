namespace FSH.Modules.Claims.Domain;

/// <summary>
/// A snapshot of one super-bill procedure line, copied from <c>ReportProcedureItem</c> at claim
/// creation/refresh time. Snapshot, not FK — the procedure/diagnostic ids are values, never joined.
/// </summary>
public sealed class ClaimLine
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid ProcedureCodeId { get; private set; }
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Charge { get; private set; }

    public IReadOnlyList<Guid> DiagnosticIds => _diagnosticIds;
    private readonly List<Guid> _diagnosticIds = [];

    private ClaimLine() { }

    public static ClaimLine Create(
        Guid claimId, Guid procedureCodeId, string code, string? description, decimal charge,
        IEnumerable<Guid> diagnosticIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var line = new ClaimLine
        {
            Id = Guid.CreateVersion7(),
            ClaimId = claimId,
            ProcedureCodeId = procedureCodeId,
            Code = code,
            Description = description,
            Charge = charge,
        };
        line._diagnosticIds.AddRange(diagnosticIds ?? []);
        return line;
    }
}
