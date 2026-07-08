namespace FSH.Modules.Administration.Domain;

/// <summary>Associates a tenant <see cref="DiagnosticCategory"/> with a global <see cref="Diagnostic"/>
/// (legacy <c>ascDiagnosticCategories</c> — <c>adcDxCategoryID</c>/<c>adcDxCodeID</c>). Many-to-many;
/// bare int code id (Diagnostic is IGlobalEntity). Tenant-scoped like the category. Hard delete on
/// replace (pure join row).</summary>
public sealed class DiagnosticCategoryCode
{
    public Guid DiagnosticCategoryId { get; private set; }
    public int DiagnosticId { get; private set; }

    private DiagnosticCategoryCode() { }

    public static DiagnosticCategoryCode Create(Guid diagnosticCategoryId, int diagnosticId) =>
        new() { DiagnosticCategoryId = diagnosticCategoryId, DiagnosticId = diagnosticId };
}
