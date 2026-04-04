using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using FluentAssertions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClinicManagementSystem.API.IntegrationTests;

public class ApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private bool _authenticated;

    public ApiEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_authenticated)
            return;

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin.test@clinic.local",
            password = "AdminTest@12345!"
        });

        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("token").GetString();

        token.Should().NotBeNullOrWhiteSpace();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _authenticated = true;
    }

    [Fact]
    public async Task GetPatients_ShouldReturnSuccessAndSeededData()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/Patients");

        response.EnsureSuccessStatusCode();
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();

        patients.Should().NotBeNull();
        patients!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAppointments_ShouldReturnSuccessAndSeededData()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/Appointments");

        response.EnsureSuccessStatusCode();
        var appointments = await response.Content.ReadFromJsonAsync<List<Appointment>>();

        appointments.Should().NotBeNull();
        appointments!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldReturnExpectedShape()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/Dashboard/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummary>();

        summary.Should().NotBeNull();
        summary!.TotalPatients.Should().BeGreaterThan(0);
        summary.TotalAppointments.Should().BeGreaterThan(0);
    }
}
