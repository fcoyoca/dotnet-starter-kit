using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateRecurringReservation;

public sealed class CreateRecurringReservationCommandValidator : AbstractValidator<CreateRecurringReservationCommand>
{
    /// <summary>Upper bound on materialized blocks (≈ one daily occurrence per day for a year).</summary>
    private const int MaxOccurrences = 366;

    public CreateRecurringReservationCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.Occurrences)
            .NotEmpty().WithMessage("At least one occurrence is required.")
            .Must(o => o.Count <= MaxOccurrences)
            .WithMessage($"A recurring reservation cannot exceed {MaxOccurrences} occurrences.");
        RuleForEach(x => x.Occurrences)
            .Must(o => o.EndUtc > o.StartUtc)
            .WithMessage("Each occurrence must end after it starts.");
    }
}
