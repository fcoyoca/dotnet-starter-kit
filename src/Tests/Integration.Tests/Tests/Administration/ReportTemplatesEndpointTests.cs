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

        var types = await ListTypesAsync(client);

        types.Count.ShouldBe(6);
        types.ShouldContain(t => t.Name == "Initial Evaluation");
        types.ShouldContain(t => t.Name == "Daily Visit");
    }

    [Fact]
    public async Task ListReportFields_Should_Return_ActiveFieldsForType()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var types = await ListTypesAsync(client);

        var eval = types.Single(t => t.Name == "Initial Evaluation");
        var evalFields = await ListFieldsAsync(client, eval.Id);
        evalFields.ShouldContain(f => f.Name == "Chief Complaint");

        // Daily Visit has inactive fields (e.g. ADL) which must be excluded.
        var daily = types.Single(t => t.Name == "Daily Visit");
        var dailyFields = await ListFieldsAsync(client, daily.Id);
        dailyFields.ShouldContain(f => f.Name == "Objective");
        dailyFields.ShouldNotContain(f => f.Name == "ADL");
    }

    [Fact]
    public async Task ListReportFields_Should_Follow_LegacyClinicalSequence()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var types = await ListTypesAsync(client);

        // Legacy ReportTypes.xml orders categories clinically (rcOrder), not alphabetically —
        // filtered to the seeded names so custom fields created by other tests don't interfere.
        string[] evalSequence =
        [
            "Chief Complaint", "Present Problem", "Medical History", "Family History",
            "Personal / Social History", "Allergies", "Medications", "Systems Review",
            "Comments", "Diagnostic Imaging", "Clinical Impression",
            "Short Term Goals", "Long Term Goals", "Plan", "Work Status or Restrictions",
        ];
        var eval = types.Single(t => t.Name == "Initial Evaluation");
        var evalNames = (await ListFieldsAsync(client, eval.Id))
            .Select(f => f.Name).Where(evalSequence.Contains).ToList();
        evalNames.ShouldBe(evalSequence);

        // Daily Visit follows SOAP order.
        string[] soapSequence = ["Subjective", "Objective", "Assessment", "Plan"];
        var daily = types.Single(t => t.Name == "Daily Visit");
        var dailyNames = (await ListFieldsAsync(client, daily.Id))
            .Select(f => f.Name).Where(soapSequence.Contains).ToList();
        dailyNames.ShouldBe(soapSequence);
    }

    [Fact]
    public async Task ListMacros_Should_Project_ReportFieldName_And_FilterByField()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var types = await ListTypesAsync(client);
        var eval = types.Single(t => t.Name == "Initial Evaluation");
        var field = (await ListFieldsAsync(client, eval.Id)).Single(f => f.Name == "Chief Complaint");

        var macroId = await (await client.PostAsJsonAsync($"{BasePath}/macros", new
        {
            name = $"Macro-{suffix}",
            text = "Body",
            reportFieldId = (int?)field.Id,
        })).DeserializeAsync<Guid>();

        var page = await (await client.GetAsync($"{BasePath}/macros?reportFieldId={field.Id}&pageSize=200"))
            .DeserializeAsync<PagedResult<MacroRow>>();

        var row = page.Items.Single(m => m.Id == macroId);
        row.ReportFieldId.ShouldBe(field.Id);
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

    [Fact]
    public async Task ListMacros_Should_RoundTrip_And_FilterBy_UseableByUser()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var owner = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var ownedId = await (await client.PostAsJsonAsync($"{BasePath}/macros", new
        {
            name = $"Owned-{suffix}",
            reportFieldId = (int?)null,
            useableByUserId = owner,
        })).DeserializeAsync<Guid>();

        // Filtering by the owner returns the owned macro carrying its UseableByUserId.
        var owned = await (await client.GetAsync($"{BasePath}/macros?useableByUserId={owner}&pageSize=200"))
            .DeserializeAsync<PagedResult<MacroRow>>();
        owned.Items.ShouldContain(m => m.Id == ownedId && m.UseableByUserId == owner);
        owned.Items.ShouldAllBe(m => m.UseableByUserId == owner);
    }

    [Fact]
    public async Task ReportType_Crud_RoundTrip()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = $"Custom Type {Guid.NewGuid():N}";

        var id = await (await client.PostAsJsonAsync($"{BasePath}/report-types", new { name, displayOrder = 9 }))
            .DeserializeAsync<int>();

        (await ListTypesAsync(client)).ShouldContain(t => t.Id == id && t.Name == name);

        var renamed = name + " (edited)";
        (await client.PutAsJsonAsync($"{BasePath}/report-types/{id}",
            new { id, name = renamed, displayOrder = 9, isActive = true }))
            .EnsureSuccessStatusCode();
        (await ListTypesAsync(client)).ShouldContain(t => t.Id == id && t.Name == renamed);

        (await client.DeleteAsync($"{BasePath}/report-types/{id}")).EnsureSuccessStatusCode();
        (await ListTypesAsync(client)).ShouldNotContain(t => t.Id == id);
    }

    [Fact]
    public async Task ReportField_Crud_RoundTrip_And_InactiveFiltering()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var types = await ListTypesAsync(client);
        var typeId = types.Single(t => t.Name == "Initial Evaluation").Id;
        var name = $"Custom Field {Guid.NewGuid():N}";

        var id = await (await client.PostAsJsonAsync($"{BasePath}/report-fields",
                new { reportTypeId = typeId, name, category = "Custom", displayOrder = 3 }))
            .DeserializeAsync<int>();

        (await ListFieldsAsync(client, typeId)).ShouldContain(f => f.Id == id && f.Name == name);

        // Deactivate -> drops out of the active list, but appears with includeInactive.
        (await client.PutAsJsonAsync($"{BasePath}/report-fields/{id}",
            new { id, name, category = "Custom", displayOrder = 3, isActive = false }))
            .EnsureSuccessStatusCode();
        (await ListFieldsAsync(client, typeId)).ShouldNotContain(f => f.Id == id);
        var withInactive = await (await client.GetAsync($"{BasePath}/report-fields?reportTypeId={typeId}&includeInactive=true"))
            .DeserializeAsync<List<ReportFieldRow>>();
        withInactive.ShouldContain(f => f.Id == id);

        (await client.DeleteAsync($"{BasePath}/report-fields/{id}")).EnsureSuccessStatusCode();
        var afterDelete = await (await client.GetAsync($"{BasePath}/report-fields?reportTypeId={typeId}&includeInactive=true"))
            .DeserializeAsync<List<ReportFieldRow>>();
        afterDelete.ShouldNotContain(f => f.Id == id);
    }

    private static async Task<List<ReportTypeRow>> ListTypesAsync(HttpClient client) =>
        await (await client.GetAsync($"{BasePath}/report-types")).DeserializeAsync<List<ReportTypeRow>>();

    private static async Task<List<ReportFieldRow>> ListFieldsAsync(HttpClient client, int typeId) =>
        await (await client.GetAsync($"{BasePath}/report-fields?reportTypeId={typeId}"))
            .DeserializeAsync<List<ReportFieldRow>>();

    private sealed record ReportTypeRow(int Id, string Name);

    private sealed record ReportFieldRow(int Id, string Name, string? Category);

    private sealed record MacroRow(
        Guid Id,
        string Name,
        string? Text,
        int? ReportFieldId,
        string? ReportFieldName,
        string? ReportCategory,
        Guid? UseableByUserId,
        bool IsActive);
}
