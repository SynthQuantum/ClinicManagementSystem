using ClinicManagementSystem.Models.DTOs;
using ClinicManagementSystem.Models.Entities;
using ClinicManagementSystem.Models.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;

namespace ClinicManagementSystem.API.IntegrationTests;

public class ApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private bool _authenticated;
    private static string? _cachedBearerToken;
    private static readonly SemaphoreSlim AuthenticationLock = new(1, 1);

    public ApiEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_authenticated)
            return;

        if (!string.IsNullOrWhiteSpace(_cachedBearerToken))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedBearerToken);
            _authenticated = true;
            return;
        }

        await AuthenticationLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedBearerToken))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedBearerToken);
                _authenticated = true;
                return;
            }

            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "admin.test@clinic.local",
                password = "AdminTest@12345!"
            });

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var token = doc.RootElement.GetProperty("token").GetString();

            token.Should().NotBeNullOrWhiteSpace();
            _cachedBearerToken = token;
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _authenticated = true;
        }
        finally
        {
            AuthenticationLock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // Patients
    // -----------------------------------------------------------------------

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
    public async Task CreatePatient_ShouldReturn201_WithLocationHeader()
    {
        await EnsureAuthenticatedAsync();

        var response = await _client.PostAsJsonAsync("/api/Patients", new
        {
            firstName = "Integration",
            lastName = "NewPatient",
            dateOfBirth = "1985-03-20T00:00:00Z",
            gender = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<Patient>();
        created.Should().NotBeNull();
        created!.FirstName.Should().Be("Integration");
    }

    [Fact]
    public async Task GetPatientById_ShouldReturn404_WhenPatientDoesNotExist()
    {
        await EnsureAuthenticatedAsync();

        var response = await _client.GetAsync($"/api/Patients/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPatients_ShouldReturn401_WhenNotAuthenticated()
    {
        // Create a fresh in-process client with no auth header
        using var unauthClient = _factory.CreateClient();
        unauthClient.DefaultRequestHeaders.Authorization = null;

        var response = await unauthClient.GetAsync("/api/Patients");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // -----------------------------------------------------------------------
    // Staff Members  (Admin role required)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStaffMembers_WithAdminAuth_ShouldReturnOk()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/StaffMembers");

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<List<StaffMember>>();
        members.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateStaffMember_WithAdminAuth_ShouldReturn201()
    {
        await EnsureAuthenticatedAsync();

        var response = await _client.PostAsJsonAsync("/api/StaffMembers", new
        {
            firstName = "Integration",
            lastName = "NewDoctor",
            email = $"newdoctor_{Guid.NewGuid():N}@test.local",
            role = (int)UserRole.Doctor,
            specialty = "Integration Testing",
            isAvailable = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<StaffMember>();
        created.Should().NotBeNull();
        created!.Specialty.Should().Be("Integration Testing");
    }

    // -----------------------------------------------------------------------
    // Appointments
    // -----------------------------------------------------------------------

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
    public async Task GetAppointmentById_ShouldReturn404_WhenNotFound()
    {
        await EnsureAuthenticatedAsync();

        var response = await _client.GetAsync($"/api/Appointments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // -----------------------------------------------------------------------
    // Dashboard
    // -----------------------------------------------------------------------

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

    [Fact]
    public async Task GetDashboardTrend_ShouldReturnTrendPoints()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/Dashboard/trend?days=7");

        response.EnsureSuccessStatusCode();
        var trend = await response.Content.ReadFromJsonAsync<List<AppointmentTrendPoint>>();

        trend.Should().NotBeNull();
        trend!.Count.Should().Be(8); // days + 1 (including today)
    }

    [Fact]
    public async Task GetDashboardStaffWorkload_ShouldReturnWorkloadList()
    {
        await EnsureAuthenticatedAsync();
        var response = await _client.GetAsync("/api/Dashboard/staff-workload");

        response.EnsureSuccessStatusCode();
        var workload = await response.Content.ReadFromJsonAsync<List<StaffWorkloadSummary>>();

        workload.Should().NotBeNull();
    }

    // -----------------------------------------------------------------------
    // Predictions
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PredictNoShow_ShouldReturnValidPredictionOutput()
    {
        await EnsureAuthenticatedAsync();

        var response = await _client.PostAsJsonAsync("/api/Predictions/no-show", new
        {
            patientAge = 35,
            daysBetweenBookingAndAppointment = 7,
            previousNoShowCount = 1,
            previousCompletedCount = 5,
            appointmentType = (int)AppointmentType.General,
            dayOfWeek = (int)DayOfWeek.Monday,
            hasInsurance = true,
            hasReminderSent = false
        });

        response.EnsureSuccessStatusCode();
        var output = await response.Content.ReadFromJsonAsync<NoShowPredictionOutput>();

        output.Should().NotBeNull();
        output!.RiskLevel.Should().BeOneOf("Low", "Medium", "High");
        output.Probability.Should().BeInRange(0m, 1m);
        output.Recommendation.Should().NotBeNullOrWhiteSpace();
    }
}
