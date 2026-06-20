using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.DeleteSmokingStatus;

public sealed class DeleteSmokingStatusCommandValidator : AbstractValidator<DeleteSmokingStatusCommand>
{
    public DeleteSmokingStatusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
