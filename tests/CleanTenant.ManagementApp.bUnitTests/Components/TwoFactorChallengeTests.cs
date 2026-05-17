using CleanTenant.ManagementApp.Components.Pages;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary><see cref="TwoFactorChallenge"/> sayfa testleri.</summary>
public sealed class TwoFactorChallengeTests : MudTestContextBase
{
    [Fact]
    public void Token_yoksa_error_alert_render_edilmeli()
    {
        var cut = RenderComponent<TwoFactorChallenge>();

        cut.Markup.Should().Contain("Geçersiz challenge token");
    }
}
