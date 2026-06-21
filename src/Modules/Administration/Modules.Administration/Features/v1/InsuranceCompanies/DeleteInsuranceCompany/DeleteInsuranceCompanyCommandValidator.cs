using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.DeleteInsuranceCompany;

public sealed class DeleteInsuranceCompanyCommandValidator : AbstractValidator<DeleteInsuranceCompanyCommand>
{
    public DeleteInsuranceCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
