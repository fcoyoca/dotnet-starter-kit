using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;

public sealed class GetReportProceduresQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetReportProceduresQuery, SuperBillDto>
{
    public async ValueTask<SuperBillDto> Handle(GetReportProceduresQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool reportExists = await dbContext.PatientReports
            .AnyAsync(x => x.Id == query.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
        if (!reportExists)
        {
            throw new NotFoundException($"Report {query.ReportId} not found.");
        }

        SuperBill? bill = await dbContext.SuperBills
            .AsNoTracking()
            .Include(x => x.Procedures)
            .ThenInclude(p => p.Diagnostics)
            .FirstOrDefaultAsync(x => x.ReportId == query.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (bill is null)
        {
            return new SuperBillDto(null, query.ReportId, false, null, []);
        }

        List<SuperBillProcedureDto> procedures = [.. bill.Procedures
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new SuperBillProcedureDto(
                p.Id,
                p.ProcedureCodeId,
                p.Code,
                p.Description,
                p.Charge,
                p.DisplayOrder,
                [.. p.Diagnostics.Select(d => d.DiagnosticId)]))];

        return new SuperBillDto(bill.Id, bill.ReportId, bill.IsBilled, bill.BilledDateUtc, procedures);
    }
}
