using CleanTenant.ManagementApp.Components.Pages;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary><see cref="About"/> sayfa testleri — sürüm/build bilgisi.</summary>
public sealed class AboutTests : MudTestContextBase
{
    [Fact]
    public void Hakkinda_basligini_ve_uygulama_adini_render_etmeli()
    {
        var cut = RenderComponent<About>();

        cut.Markup.Should().Contain("Hakkında");
        cut.Markup.Should().Contain("CleanTenant Yönetim");
    }

    [Fact]
    public void Faz_0_ciktilarini_listelemeli()
    {
        var cut = RenderComponent<About>();

        cut.Markup.Should().Contain("Auth + 2FA + Multi-scope");
        cut.Markup.Should().Contain("MediatR pipeline");
        cut.Markup.Should().Contain("Audit Interceptor");
        cut.Markup.Should().Contain("146 yeşil test");
    }
}
