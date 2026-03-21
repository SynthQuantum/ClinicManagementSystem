using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using FluentAssertions;
using System.Net.Http.Json;

namespace ClinicManagementSystem.API.IntegrationTests;

public class ApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPatients_ShouldReturnSuccessAndSeededData()
    {
        var response = await _client.GetAsync("/api/Patients");

        response.EnsureSuccessStatusCode();
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();

        patients.Should().NotBeNull();
        patients!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAppointments_ShouldReturnSuccessAndSeededData()
    {
        var response = await _client.GetAsync("/api/Appointments");

        response.EnsureSuccessStatusCode();
        var appointments = await response.Content.ReadFromJsonAsync<List<Appointment>>();

        appointments.Should().NotBeNull();
        appointments!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDashboardSummary_ShouldReturnExpectedShape()
    {
        var response = await _client.GetAsync("/api/Dashboard/summary");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummary>();

        summary.Should().NotBeNull();
        summary!.TotalPatients.Should().BeGreaterThan(0);
        summary.TotalAppointments.Should().BeGreaterThan(0);
    }
}
