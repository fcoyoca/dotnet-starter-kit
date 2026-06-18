using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.UpdateSmokingStatus;

public sealed class UpdateSmokingStatusCommandValidator : AbstractValidator<UpdateSmokingStatusCommand>
{
    public UpdateSmokingStatusCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SnomedCode).MaximumLength(64);
    }
}
