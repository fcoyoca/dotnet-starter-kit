using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Administration;

[Collection(FshCollectionDefinition.Name)]
public sealed class CodeSourcesEndpointTests
{
    private const string BasePath = "/api/v1/administration";

    private readonly AuthHelper _auth;

    public CodeSourcesEndpointTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ListCodeSources_Should_Include_SeededDefaults()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var rows = await (await client.GetAsync($"{BasePath}/code-sources")).DeserializeAsync<List<LookupRow>>();

        rows.ShouldContain(r => r.Name == "CPT");
        rows.ShouldContain(r => r.Name == "HCPCS");
    }

    [Fact]
    public async Task ListProcedureCodes_Should_Project_CodeSourceName()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var categoryId = await (await client.PostAsJsonAsync($"{BasePath}/procedure-categories", new
        {
            name = $"Cat-{suffix}",
            description = (string?)null,
            isImaging = false,
        })).DeserializeAsync<Guid>();

        // Pick a real seeded code source by name so the test is independent of seed ids.
        var sources = await (await client.GetAsync($"{BasePath}/code-sources")).DeserializeAsync<List<LookupRow>>();
        var cpt = sources.Single(s => s.Name == "CPT");

        var codeId = await (await client.PostAsJsonAsync($"{BasePath}/procedure-codes", new
        {
            code = $"PC-{suffix}",
            procedureCategoryId = categoryId,
            name = (string?)null,
            description = (string?)null,
            codeSourceId = (int?)cpt.Id,
            macroText = (string?)null,
        })).DeserializeAsync<Guid>();

        var page = await (await client.GetAsync(
                $"{BasePath}/procedure-codes?procedureCategoryId={categoryId}&pageNumber=1&pageSize=20"))
            .DeserializeAsync<PagedResult<ProcedureCodeRow>>();

        var row = page.Items.ShouldHaveSingleItem();
        row.Id.ShouldBe(codeId);
        row.CodeSourceId.ShouldBe(cpt.Id);
        row.CodeSourceName.ShouldBe("CPT");
    }

    private sealed record LookupRow(int Id, string Name, bool IsActive);

    private sealed record ProcedureCodeRow(
        Guid Id,
        string Code,
        Guid ProcedureCategoryId,
        string? ProcedureCategoryName,
        int? CodeSourceId,
        string? CodeSourceName);
}
