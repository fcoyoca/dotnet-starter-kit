using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;

public static class ImportDrugsEndpoint
{
    internal static RouteHandlerBuilder MapImportDrugsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/drugs/import",
                async (ImportDrugsCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("ImportDrugs")
            .WithSummary("Import selected RxNav search results into the local drug catalog (upsert by RxCui)")
            .RequirePermission(AdministrationPermissions.Drugs.Create);
    }
}
