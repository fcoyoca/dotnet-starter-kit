using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record ImportDrugsCommand(IReadOnlyList<RxNavDrugDto> Items) : ICommand<int>;
