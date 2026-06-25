using FSH.Modules.Scheduling.Contracts.Dtos;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Scheduling;

[Collection(FshCollectionDefinition.Name)]
public sealed class AppointmentsEndpointTests
{
    private const string Appointments = $"{TestConstants.SchedulingBasePath}/appointments";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public AppointmentsEndpointTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_Should_Return200_And_Persist_When_RefsValid()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);

        var id = await CreateAppointmentAsync(client, clinicId, providerId, start, start.AddMinutes(30), "Annual checkup");

        var dto = await (await client.GetAsync($"{Appointments}/{id}")).DeserializeAsync<AppointmentDto>();
        dto.ClinicId.ShouldBe(clinicId);
        dto.ProviderId.ShouldBe(providerId);
        dto.StartUtc.ShouldBe(start);
        dto.EndUtc.ShouldBe(start.AddMinutes(30));
        dto.Notes.ShouldBe("Annual checkup");
        dto.Status.ShouldBe("Scheduled");
        dto.Cancelled.ShouldBeFalse();
        dto.NoShow.ShouldBeFalse();
    }

    [Fact]
    public async Task ListAppointments_Should_Return_Only_AppointmentsOverlappingRange()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);
        var id = await CreateAppointmentAsync(client, clinicId, providerId, start, start.AddHours(1), null);

        // Window covering the appointment day → included.
        var inRange = await ListAsync(client, clinicId,
            new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc));
        inRange.ShouldContain(a => a.Id == id);

        // Window on a different day → excluded.
        var outOfRange = await ListAsync(client, clinicId,
            new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc));
        outOfRange.ShouldNotContain(a => a.Id == id);
    }

    [Fact]
    public async Task Lifecycle_CheckIn_Then_CheckOut_Advances_Status()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var id = await CreateAppointmentAsync(client, clinicId, providerId, start, start.AddMinutes(30), null);

        (await client.PostAsync($"{Appointments}/{id}/check-in", content: null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync(client, id)).Status.ShouldBe("CheckedIn");

        (await client.PostAsync($"{Appointments}/{id}/check-out", content: null))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync(client, id)).Status.ShouldBe("CheckedOut");
    }

    [Fact]
    public async Task Cancel_And_NoShow_Set_Flags()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 4, 11, 0, 0, DateTimeKind.Utc);

        var cancelId = await CreateAppointmentAsync(client, clinicId, providerId, start, start.AddMinutes(30), null);
        await client.PostAsync($"{Appointments}/{cancelId}/cancel", content: null);
        (await GetAsync(client, cancelId)).Cancelled.ShouldBeTrue();

        var noShowId = await CreateAppointmentAsync(client, clinicId, providerId, start.AddHours(1), start.AddHours(1).AddMinutes(30), null);
        await client.PostAsync($"{Appointments}/{noShowId}/no-show", content: null);
        (await GetAsync(client, noShowId)).NoShow.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAppointment_Should_SoftDelete_And_HideFromRangeList()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 6, 8, 0, 0, DateTimeKind.Utc);
        var id = await CreateAppointmentAsync(client, clinicId, providerId, start, start.AddMinutes(30), null);

        (await client.DeleteAsync($"{Appointments}/{id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync($"{Appointments}/{id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var list = await ListAsync(client, clinicId,
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc));
        list.ShouldNotContain(a => a.Id == id, "Soft-deleted appointments must be excluded by the query filter.");
    }

    // ─── validation + refs ───────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_Should_Return404_When_ClinicDoesNotExist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc);

        var response = await client.PostAsJsonAsync(Appointments, new
        {
            clinicId = Guid.NewGuid(), // unknown clinic
            providerId,
            patientId = (Guid?)null,
            appointmentTypeId = (Guid?)null,
            startUtc = start,
            endUtc = start.AddMinutes(30),
            notes = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAppointment_Should_Return400_When_EndNotAfterStart()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var (clinicId, providerId) = await CreateClinicAndProviderAsync(client);
        var start = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc);

        var response = await client.PostAsJsonAsync(Appointments, new
        {
            clinicId,
            providerId,
            patientId = (Guid?)null,
            appointmentTypeId = (Guid?)null,
            startUtc = start,
            endUtc = start, // equal → invalid
            notes = (string?)null,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task ListAppointments_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.GetAsync(
            $"{Appointments}?clinicId={Guid.NewGuid()}&fromUtc=2026-07-01T00:00:00Z&toUtc=2026-07-02T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static async Task<(Guid ClinicId, Guid ProviderId)> CreateClinicAndProviderAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clinicResponse = await client.PostAsJsonAsync($"{TestConstants.AdministrationBasePath}/clinics", new
        {
            code = $"C-{suffix}",
            name = $"Clinic {suffix}",
            address1 = "1 Test St",
            address2 = (string?)null,
            city = "Springfield",
            state = "IL",
            zip = "62704",
            phone = (string?)null,
            timeZoneId = "America/Chicago",
        });
        clinicResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var clinicId = await clinicResponse.DeserializeAsync<Guid>();

        var providerResponse = await client.PostAsJsonAsync($"{TestConstants.AdministrationBasePath}/providers", new
        {
            firstName = "Test",
            lastName = $"Provider-{suffix}",
            primaryClinicId = clinicId,
        });
        providerResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var providerId = await providerResponse.DeserializeAsync<Guid>();

        return (clinicId, providerId);
    }

    private static async Task<Guid> CreateAppointmentAsync(
        HttpClient client, Guid clinicId, Guid providerId, DateTime startUtc, DateTime endUtc, string? notes)
    {
        var response = await client.PostAsJsonAsync(Appointments, new
        {
            clinicId,
            providerId,
            patientId = (Guid?)null,
            appointmentTypeId = (Guid?)null,
            startUtc,
            endUtc,
            notes,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<AppointmentDto> GetAsync(HttpClient client, Guid id) =>
        await (await client.GetAsync($"{Appointments}/{id}")).DeserializeAsync<AppointmentDto>();

    private static async Task<IReadOnlyList<AppointmentDto>> ListAsync(HttpClient client, Guid clinicId, DateTime fromUtc, DateTime toUtc)
    {
        var url = $"{Appointments}?clinicId={clinicId}" +
                  $"&fromUtc={Uri.EscapeDataString(fromUtc.ToString("O"))}" +
                  $"&toUtc={Uri.EscapeDataString(toUtc.ToString("O"))}";
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.DeserializeAsync<List<AppointmentDto>>();
    }
}
