using CleanTenant.ManagementApp.Themes;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// Kullanıcının tema tercihini (preset + dark mode) okuyan ve kaydeden sözleşme.
/// v0.2.1'de <c>localStorage</c> implementasyonu var; Faz 1+ kullanıcı profili
/// DB'de de saklanabilir.
/// </summary>
public interface IThemeService
{
    /// <summary>Aktif preset.</summary>
    ThemePresetId CurrentPreset { get; }

    /// <summary>Aktif dark mode bayrağı.</summary>
    bool IsDarkMode { get; }

    /// <summary>Tema değişimi sonrası UI'ya re-render sinyali — bileşenler bu event'i dinler.</summary>
    event Action? ThemeChanged;

    /// <summary>localStorage'dan tercihi okur ve aktif yapar (App initialization).</summary>
    Task InitializeAsync();

    /// <summary>Preset değiştir + localStorage'a yaz + event tetikle.</summary>
    Task SetPresetAsync(ThemePresetId preset);

    /// <summary>Dark/light toggle + localStorage'a yaz + event tetikle.</summary>
    Task ToggleDarkModeAsync();
}
