using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record UpdateDrugCommand(
    int Id,
    string Name,
    string? RxAui,
    string? RxCui,
    string? Tty,
    string? Sab,
    string? Code,
    bool IsActive) : ICommand<Unit>;
