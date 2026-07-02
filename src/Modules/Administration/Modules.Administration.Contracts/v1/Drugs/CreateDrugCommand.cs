using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record CreateDrugCommand(
    string Name,
    string? RxAui = null,
    string? RxCui = null,
    string? Tty = null,
    string? Sab = null,
    string? Code = null) : ICommand<int>;
