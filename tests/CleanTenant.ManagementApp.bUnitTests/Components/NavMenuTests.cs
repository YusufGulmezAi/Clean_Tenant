using CleanTenant.ManagementApp.Components.Layout;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// <see cref="NavMenu"/> drawer içeriği testleri. v0.2.3.c'den itibaren NavMenu
/// scope-aware: Sistem / Yönetim / Site bağlamına göre dinamik menü render eder.
/// Fixture anonim auth state kullandığı için OnInitializedAsync DB sorgusu
/// yapmadan erken döner; bu testler scope-bağımsız "her zaman görünen" öğeleri
/// kapsar (Dashboard, Ayarlar grubu).
/// </summary>
public sealed class NavMenuTests : MudTestContextBase
{
    /// <summary>Anonim user için Dashboard linki her zaman görünür.</summary>
    [Fact]
    public void Dashboard_link_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Dashboard");
    }

    /// <summary>Ayarlar NavGroup tüm bağlamlarda altta görünür.</summary>
    [Fact]
    public void Ayarlar_grup_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Ayarlar");
    }

    /// <summary>Ayarlar altındaki Tema/Dil/Hakkında alt linkleri.</summary>
    [Fact]
    public void Ayarlar_alt_linkler_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Tema");
        cut.Markup.Should().Contain("Dil");
        cut.Markup.Should().Contain("Hakkında");
    }

    /// <summary>Üstteki canlı arama kutusu daima render edilir.</summary>
    [Fact]
    public void Arama_kutusu_render_edilmeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().Contain("Menüde ara");
    }

    /// <summary>
    /// Anonim user için scope-bağımlı NavGroup'lar render edilmez (Yönetimler,
    /// Siteler, Sistem Yönetimi vb. yalnız ilgili scope'ta görünür).
    /// </summary>
    [Fact]
    public void Anonim_user_icin_scope_baglamli_gruplar_gorunmemeli()
    {
        var cut = RenderComponent<NavMenu>();

        cut.Markup.Should().NotContain("Yönetimler");
        cut.Markup.Should().NotContain("Sistem Yönetimi");
    }
}
