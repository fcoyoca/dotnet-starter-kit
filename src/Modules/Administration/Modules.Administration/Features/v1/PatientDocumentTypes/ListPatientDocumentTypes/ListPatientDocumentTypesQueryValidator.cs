using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.ListPatientDocumentTypes;

public sealed class ListPatientDocumentTypesQueryValidator : AbstractValidator<ListPatientDocumentTypesQuery>
{
    public ListPatientDocumentTypesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
