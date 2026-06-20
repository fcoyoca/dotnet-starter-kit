using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Clinics;

namespace FSH.Modules.Administration.Features.v1.Clinics.DeleteClinic;

public sealed class DeleteClinicCommandValidator : AbstractValidator<DeleteClinicCommand>
{
    public DeleteClinicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
