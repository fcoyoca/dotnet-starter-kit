using System.Text.Json;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public sealed class SearchRxNavQueryHandler(IHttpClientFactory httpClientFactory)
    : IQueryHandler<SearchRxNavQuery, IReadOnlyList<RxNavDrugDto>>
{
    public async ValueTask<IReadOnlyList<RxNavDrugDto>> Handle(SearchRxNavQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        HttpClient client = httpClientFactory.CreateClient("RxNav");
        string json = await client
            .GetStringAsync(new Uri($"REST/drugs.json?name={Uri.EscapeDataString(query.Term)}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        return ParseDrugsResponse(json);
    }

    /// <summary>Maps RxNav /REST/drugs.json to candidates. Internal shape: drugGroup.conceptGroup[].conceptProperties[].</summary>
    public static IReadOnlyList<RxNavDrugDto> ParseDrugsResponse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        var results = new List<RxNavDrugDto>();

        if (!doc.RootElement.TryGetProperty("drugGroup", out JsonElement drugGroup) ||
            !drugGroup.TryGetProperty("conceptGroup", out JsonElement conceptGroups))
        {
            return results;
        }

        foreach (JsonElement group in conceptGroups.EnumerateArray())
        {
            if (!group.TryGetProperty("conceptProperties", out JsonElement properties))
            {
                continue;
            }

            foreach (JsonElement concept in properties.EnumerateArray())
            {
                string? rxCui = concept.TryGetProperty("rxcui", out JsonElement c) ? c.GetString() : null;
                string? name = concept.TryGetProperty("name", out JsonElement n) ? n.GetString() : null;
                string? tty = concept.TryGetProperty("tty", out JsonElement t) ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(rxCui) && !string.IsNullOrWhiteSpace(name))
                {
                    results.Add(new RxNavDrugDto(rxCui, name, tty));
                }
            }
        }

        return results;
    }
}
