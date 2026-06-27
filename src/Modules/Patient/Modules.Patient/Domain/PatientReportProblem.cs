namespace FSH.Modules.Patient.Domain;

/// <summary>Associates a <see cref="PatientProblem"/> with a <see cref="PatientReport"/> (legacy
/// "Associated Problems"). Bare <see cref="ProblemId"/> — no FK to the problem so deleting a problem
/// doesn't cascade into report history.</summary>
public sealed class PatientReportProblem
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid ProblemId { get; private set; }

    private PatientReportProblem() { }

    public static PatientReportProblem Create(Guid reportId, Guid problemId) =>
        new() { Id = Guid.CreateVersion7(), ReportId = reportId, ProblemId = problemId };
}
