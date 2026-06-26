namespace FSH.Modules.Patient.Contracts.Dtos;

public enum ReportWorkflowStatus
{
    Draft,
#pragma warning disable CA1720 // Identifier contains type name
    Signed,
#pragma warning restore CA1720
    ReviewRequested,
    Reviewed
}
