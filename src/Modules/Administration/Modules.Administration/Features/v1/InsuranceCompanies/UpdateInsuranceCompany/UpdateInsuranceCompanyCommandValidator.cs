using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.UpdateInsuranceCompany;

public sealed class UpdateInsuranceCompanyCommandValidator : AbstractValidator<UpdateInsuranceCompanyCommand>
{
    public UpdateInsuranceCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FormularyTiers).InclusiveBetween(0, 20);
        RuleFor(x => x.Address1).MaximumLength(100);
        RuleFor(x => x.Address2).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(50);
        RuleFor(x => x.Zip).MaximumLength(10);
        RuleFor(x => x.Phone).MaximumLength(20);
    }
}
