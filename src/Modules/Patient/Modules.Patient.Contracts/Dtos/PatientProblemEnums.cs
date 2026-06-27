namespace FSH.Modules.Patient.Contracts.Dtos;

/// <summary>Clinical status of a patient problem (legacy <c>ProblemStatuses</c>: 1=Active, 2=Resolved, 3=Inactive).</summary>
public enum ProblemStatus
{
    Active,
    Resolved,
    Inactive
}
