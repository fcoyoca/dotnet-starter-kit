using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.CreateSmokingStatus;

public sealed class CreateSmokingStatusCommandValidator : AbstractValidator<CreateSmokingStatusCommand>
{
    public CreateSmokingStatusCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.SnomedCode).MaximumLength(64);
    }
}
