using MudBlazor;

namespace CleanTenant.ManagementApp.Themes;

/// <summary>
/// 4 tema preset'inin <see cref="MudTheme"/> tanımları. Her preset light + dark
/// varyantı birlikte taşır. Tipografi + LayoutProperties tüm preset'lerde ortak.
/// </summary>
public static class CleanTenantThemes
{
    private static readonly Typography SharedTypography = new()
    {
        Default = new DefaultTypography { FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"] }
    };

    private static readonly LayoutProperties SharedLayout = new()
    {
        AppbarHeight = "64px",
        DefaultBorderRadius = "6px",
        DrawerWidthLeft = "260px",
        DrawerMiniWidthLeft = "64px",
    };

    /// <summary>Kurumsal Mavi (default) — light + dark.</summary>
    public static readonly MudTheme KurumsalMavi = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1976D2",
            Secondary = "#FF9800",
            AppbarBackground = "#1976D2",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#42A5F5",
            Secondary = "#FFB74D",
            AppbarBackground = "#0D47A1",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
        },
        Typography = SharedTypography,
        LayoutProperties = SharedLayout,
    };

    /// <summary>Teşkilatsal Yeşil — light + dark.</summary>
    public static readonly MudTheme TeskilatsalYesil = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2E7D32",
            Secondary = "#607D8B",
            AppbarBackground = "#2E7D32",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#66BB6A",
            Secondary = "#90A4AE",
            AppbarBackground = "#1B5E20",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
        },
        Typography = SharedTypography,
        LayoutProperties = SharedLayout,
    };

    /// <summary>MudBlazor Mor — light + dark.</summary>
    public static readonly MudTheme MudBlazorMor = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#594AE2",
            Secondary = "#FF4081",
            AppbarBackground = "#594AE2",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7E6BFF",
            Secondary = "#FF80AB",
            AppbarBackground = "#311B92",
            AppbarText = "#FFFFFF",
            Background = "#121212",
            Surface = "#1E1E1E",
            DrawerBackground = "#1E1E1E",
        },
        Typography = SharedTypography,
        LayoutProperties = SharedLayout,
    };

    /// <summary>Koyu Kurumsal — light + dark (dark default önerilir).</summary>
    public static readonly MudTheme KoyuKurumsal = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#263238",
            Secondary = "#00BCD4",
            AppbarBackground = "#263238",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#90A4AE",
            Secondary = "#26C6DA",
            AppbarBackground = "#000000",
            AppbarText = "#FFFFFF",
            Background = "#0D0D0D",
            Surface = "#1A1A1A",
            DrawerBackground = "#1A1A1A",
        },
        Typography = SharedTypography,
        LayoutProperties = SharedLayout,
    };

    /// <summary>Preset id'sinden tema referansını döner.</summary>
    public static MudTheme Resolve(ThemePresetId id) => id switch
    {
        ThemePresetId.TeskilatsalYesil => TeskilatsalYesil,
        ThemePresetId.MudBlazorMor => MudBlazorMor,
        ThemePresetId.KoyuKurumsal => KoyuKurumsal,
        _ => KurumsalMavi,
    };

    /// <summary>Preset'in görünür adı (UI seçicide).</summary>
    public static string DisplayName(ThemePresetId id) => id switch
    {
        ThemePresetId.KurumsalMavi => "Kurumsal Mavi",
        ThemePresetId.TeskilatsalYesil => "Teşkilatsal Yeşil",
        ThemePresetId.MudBlazorMor => "MudBlazor Mor",
        ThemePresetId.KoyuKurumsal => "Koyu Kurumsal",
        _ => "Bilinmeyen",
    };
}
