using CleanTenant.ManagementApp.Components.Pages;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// <see cref="Login"/> sayfa testleri — form render kontrolü.
/// (SupplyParameterFromQuery testleri NavigationManager pattern'ı gerektirir; v0.2.2.b'de.)
/// </summary>
public sealed class LoginTests : MudTestContextBase
{
    [Fact]
    public void Form_render_edilmeli_ve_action_sign_in_olmali()
    {
        var cut = RenderComponent<Login>();

        cut.Markup.Should().Contain("CleanTenant");
        cut.Markup.Should().Contain("Yönetim Paneline Giriş");
        cut.Markup.Should().Contain("action=\"/auth/sign-in\"");
        cut.Markup.Should().Contain("name=\"identifier\"");
        cut.Markup.Should().Contain("name=\"password\"");
        cut.Markup.Should().Contain("name=\"rememberMe\"");
    }

    [Fact]
    public void Beni_hatirla_checkbox_render_edilmeli()
    {
        var cut = RenderComponent<Login>();

        cut.Markup.Should().Contain("Beni hatırla");
        cut.Markup.Should().Contain("7 gün");
    }

    [Fact]
    public void Persona_hidden_input_Management_olmali()
    {
        var cut = RenderComponent<Login>();

        cut.Markup.Should().Contain("name=\"persona\"");
        cut.Markup.Should().Contain("value=\"Management\"");
    }
}
