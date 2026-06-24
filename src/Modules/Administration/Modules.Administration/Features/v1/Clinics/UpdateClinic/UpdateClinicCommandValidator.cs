using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Clinics;

namespace FSH.Modules.Administration.Features.v1.Clinics.UpdateClinic;

public sealed class UpdateClinicCommandValidator : AbstractValidator<UpdateClinicCommand>
{
    public UpdateClinicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address1).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address2).MaximumLength(100);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Zip).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.TimeZoneId).MaximumLength(64);
    }
}
