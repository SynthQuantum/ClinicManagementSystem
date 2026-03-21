using Bunit;
using ClinicManagementSystem.Blazor.Components.Layout;
using FluentAssertions;

namespace ClinicManagementSystem.Blazor.Tests;

public class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_ShouldRenderExpectedPrimaryNavigationLinks()
    {
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
