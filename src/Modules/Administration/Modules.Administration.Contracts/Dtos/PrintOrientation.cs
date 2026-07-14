namespace FSH.Modules.Administration.Contracts.Dtos;

/// <summary>
/// Page orientation used when printing/exporting a clinic's patient reports (legacy BackChart
/// ClientSettings <c>PRINT_ORIENTATION</c>, scoped per clinic here). Persisted as a string so the
/// column stays readable and stable if members are reordered.
/// </summary>
public enum PrintOrientation
{
    Portrait,
    Landscape,
}
