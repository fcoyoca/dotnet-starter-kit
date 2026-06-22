using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Administration;

[Collection(FshCollectionDefinition.Name)]
public sealed class ReportTemplatesEndpointTests
{
    private const string BasePath = "/api/v1/administration";

    private readonly AuthHelper _auth;

    public ReportTemplatesEndpointTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task ListReportTypes_Should_Return_SeededCatalog()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var types = await (await client.GetAsync($"{BasePath}/report-types")).DeserializeAsync<List<ReportTypeRow>>();

        types.Count.ShouldBe(6);
        types.ShouldContain(t => t.Id == 1 && t.Name == "Initial Evaluation");
    }

    [Fact]
    public async Task ListReportFields_Should_Return_FieldsForType()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        var fields = await (await client.GetAsync($"{BasePath}/report-fields?reportTypeId=1"))
            .DeserializeAsync<List<ReportFieldRow>>();

        fields.ShouldContain(f => f.Id == 1 && f.Name == "Chief Complaint");
        // Inactive fields are excluded.
        fields.ShouldNotContain(f => f.Id == 21);
    }

    [Fact]
    public async Task ListMacros_Should_Project_ReportFieldName_And_FilterByField()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var macroId = await (await client.PostAsJsonAsync($"{BasePath}/macros", new
        {
            name = $"Macro-{suffix}",
            text = "Body",
            reportFieldId = (int?)1,
        })).DeserializeAsync<Guid>();

        var page = await (await client.GetAsync($"{BasePath}/macros?reportFieldId=1&pageSize=200"))
            .DeserializeAsync<PagedResult<MacroRow>>();

        var row = page.Items.Single(m => m.Id == macroId);
        row.ReportFieldId.ShouldBe(1);
        row.ReportFieldName.ShouldBe("Chief Complaint");
        row.ReportCategory.ShouldBe("Chief Complaint");
    }

    [Fact]
    public async Task ListMacros_General_Should_Return_UnassignedMacros()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var macroId = await (await client.PostAsJsonAsync($"{BasePath}/macros", new
        {
            name = $"General-{suffix}",
            text = "Body",
            reportFieldId = (int?)null,
        })).DeserializeAsync<Guid>();

        var page = await (await client.GetAsync($"{BasePath}/macros?general=true&pageSize=200"))
            .DeserializeAsync<PagedResult<MacroRow>>();

        var row = page.Items.Single(m => m.Id == macroId);
        row.ReportFieldId.ShouldBeNull();
        row.ReportFieldName.ShouldBeNull();
    }

    private sealed record ReportTypeRow(int Id, string Name);

    private sealed record ReportFieldRow(int Id, string Name, string? Category);

    private sealed record MacroRow(
        Guid Id,
        string Name,
        string? Text,
        int? ReportFieldId,
        string? ReportFieldName,
        string? ReportCategory,
        bool IsActive);
}
