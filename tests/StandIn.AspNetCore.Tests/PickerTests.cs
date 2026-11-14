using System;
using System.Security.Claims;

namespace StandIn.AspNetCore.Tests;

public class PickerTests
{
    private static StandInOptions Options() =>
        new StandInOptions()
            .AddUser(new StandInUser("a", "Ann <Admin>").SetDescription("Voller Zugriff"))
            .AddUser(new StandInUser("b", "Ben"));

    [Fact]
    public void Picker_lists_users_and_a_logout_link()
    {
        var html = StandInPickerRenderer.RenderPicker(Options(), null, string.Empty, "/home");

        Assert.Contains("OverrideUserId=a", html, StringComparison.Ordinal);
        Assert.Contains("OverrideUserId=b", html, StringComparison.Ordinal);
        Assert.Contains("/stuntman/sign-out?ReturnUrl=%2Fhome", html, StringComparison.Ordinal);
        Assert.Contains("Logout", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Picker_encodes_names_and_descriptions()
    {
        var html = StandInPickerRenderer.RenderPicker(Options(), null, string.Empty, null);

        Assert.DoesNotContain("<Admin>", html, StringComparison.Ordinal);
        Assert.Contains("Ann &lt;Admin&gt;", html, StringComparison.Ordinal);
        Assert.Contains("Voller Zugriff Source: Local", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Picker_marks_the_active_user()
    {
        var options = Options();
        var principal = options.Users[1].ToPrincipal("StandIn");

        var html = StandInPickerRenderer.RenderPicker(options, principal, string.Empty, null);

        Assert.Contains("StandIn: Ben", html, StringComparison.Ordinal);
        Assert.Contains("class=\"standin-active\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Picker_shows_anonymous_without_a_user()
    {
        var html = StandInPickerRenderer.RenderPicker(Options(), new ClaimsPrincipal(new ClaimsIdentity()), string.Empty, null);

        Assert.Contains("StandIn: anonymous", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(StandInAlignment.Left, "standin-left")]
    [InlineData(StandInAlignment.Center, "standin-center")]
    [InlineData(StandInAlignment.Right, "standin-right")]
    public void Picker_alignment_is_rendered(StandInAlignment alignment, string cssClass)
    {
        var options = Options().SetUserPickerAlignment(alignment);

        var html = StandInPickerRenderer.RenderPicker(options, null, string.Empty, null);

        Assert.Contains($"class=\"standin {cssClass}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Picker_is_at_the_top_by_default_and_at_the_bottom_on_request()
    {
        var top = StandInPickerRenderer.RenderPicker(Options(), null, string.Empty, null);
        var bottom = StandInPickerRenderer.RenderPicker(
            Options().SetUserPickerVerticalPosition(StandInVerticalPosition.Bottom), null, string.Empty, null);

        Assert.DoesNotContain("<div class=\"standin standin-left standin-bottom\">", top, StringComparison.Ordinal);
        Assert.Contains("<div class=\"standin standin-left standin-bottom\">", bottom, StringComparison.Ordinal);
    }

    [Fact]
    public void Picker_links_include_the_path_base()
    {
        var html = StandInPickerRenderer.RenderPicker(Options(), null, "/app", null);

        Assert.Contains("href=\"/app/stuntman/sign-in?OverrideUserId=a\"", html, StringComparison.Ordinal);
    }
}
