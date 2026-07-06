using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>Gets the procedures performed (super bill) for a report; empty shell when none exists yet.</summary>
public sealed record GetReportProceduresQuery(Guid ReportId) : IQuery<SuperBillDto>;
