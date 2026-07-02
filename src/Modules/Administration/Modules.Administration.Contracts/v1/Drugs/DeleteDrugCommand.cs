using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record DeleteDrugCommand(int Id) : ICommand<Unit>;
