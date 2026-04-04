using Bunit;
using Bunit.TestDoubles;
using ClinicManagementSystem.Blazor.Components.Layout;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManagementSystem.Blazor.Tests;

public class NavMenuTests : TestContext
{
    public NavMenuTests()
    {
        Services.AddHttpContextAccessor();
        Services.AddAntiforgery();
    }

    [Fact]
    public void NavMenu_ShouldRenderExpectedPrimaryNavigationLinks()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@clinic.local");
        authContext.SetRoles("Admin");

        var cut = RenderComponent<NavMenu>();
        var markup = cut.Markup;

        markup.Should().Contain("Dashboard");
        markup.Should().Contain("Patients");
        markup.Should().Contain("Appointments");
        markup.Should().Contain("Staff");
        markup.Should().Contain("Prediction Lab");
        markup.Should().Contain("ML Metrics");
        markup.Should().Contain("Settings");
    }
}
