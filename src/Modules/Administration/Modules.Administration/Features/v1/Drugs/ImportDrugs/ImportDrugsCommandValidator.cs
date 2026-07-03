using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;

public sealed class ImportDrugsCommandValidator : AbstractValidator<ImportDrugsCommand>
{
    public ImportDrugsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.RxCui).NotEmpty().MaximumLength(12);
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(2048);
            item.RuleFor(i => i.Tty).MaximumLength(20);
        });
    }
}
