using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Administration;

[Collection(FshCollectionDefinition.Name)]
public sealed class InsuranceTypeProceduresEndpointTests
{
    private const string BasePath = "/api/v1/administration";

    private readonly AuthHelper _auth;

    public InsuranceTypeProceduresEndpointTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task SetProcedures_Then_List_Should_Return_Saved_Associations_With_Price()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var categoryId = await CreateCategoryAsync(client, suffix);
        var codeId = await CreateCodeAsync(client, suffix, "A", categoryId);
        var typeId = await CreateTypeAsync(client, suffix, categoryId);

        var setResponse = await client.PutAsJsonAsync($"{BasePath}/insurance-types/{typeId}/procedures", new
        {
            insuranceTypeId = typeId,
            items = new[] { new { procedureCodeId = codeId, price = 42.50m } },
        });
        setResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // This GET is the path that produced "Could not load associated procedure codes": list the
        // associations once real rows exist. It must translate and return the saved row.
        var listResponse = await client.GetAsync($"{BasePath}/insurance-types/{typeId}/procedures");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var rows = await listResponse.DeserializeAsync<List<ItpRow>>();
        var row = rows.ShouldHaveSingleItem();
        row.ProcedureCodeId.ShouldBe(codeId);
        row.ProcedureCode.ShouldBe($"A-{suffix}");
        row.Price.ShouldBe(42.50m);
    }

    [Fact]
    public async Task SetProcedures_Should_Replace_Existing_Set()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var categoryId = await CreateCategoryAsync(client, suffix);
        var codeA = await CreateCodeAsync(client, suffix, "A", categoryId);
        var codeB = await CreateCodeAsync(client, suffix, "B", categoryId);
        var typeId = await CreateTypeAsync(client, suffix, categoryId);

        await SetAsync(client, typeId, (codeA, 10m));
        await SetAsync(client, typeId, (codeB, 20m));

        var rows = await (await client.GetAsync($"{BasePath}/insurance-types/{typeId}/procedures"))
            .DeserializeAsync<List<ItpRow>>();
        var row = rows.ShouldHaveSingleItem();
        row.ProcedureCodeId.ShouldBe(codeB);
        row.Price.ShouldBe(20m);
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string suffix) =>
        await (await client.PostAsJsonAsync($"{BasePath}/procedure-categories", new
        {
            name = $"Cat-{suffix}",
            description = (string?)null,
            isImaging = false,
        })).DeserializeAsync<Guid>();

    private static async Task<Guid> CreateCodeAsync(HttpClient client, string suffix, string label, Guid categoryId) =>
        await (await client.PostAsJsonAsync($"{BasePath}/procedure-codes", new
        {
            code = $"{label}-{suffix}",
            procedureCategoryId = categoryId,
            name = (string?)null,
            description = (string?)null,
            codeSource = (string?)null,
            macroText = (string?)null,
        })).DeserializeAsync<Guid>();

    private static async Task<Guid> CreateTypeAsync(HttpClient client, string suffix, Guid categoryId) =>
        await (await client.PostAsJsonAsync($"{BasePath}/insurance-types", new
        {
            name = $"PPO-{suffix}",
            procedureCategoryId = (Guid?)categoryId,
        })).DeserializeAsync<Guid>();

    private static async Task SetAsync(HttpClient client, Guid typeId, params (Guid CodeId, decimal Price)[] items)
    {
        var response = await client.PutAsJsonAsync($"{BasePath}/insurance-types/{typeId}/procedures", new
        {
            insuranceTypeId = typeId,
            items = items.Select(i => new { procedureCodeId = i.CodeId, price = i.Price }).ToArray(),
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private sealed record ItpRow(
        Guid Id,
        Guid InsuranceTypeId,
        Guid ProcedureCodeId,
        string ProcedureCode,
        string? ProcedureName,
        string? ProcedureCategoryName,
        decimal Price);
}
