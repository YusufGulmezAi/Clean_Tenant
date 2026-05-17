using Blazored.LocalStorage;
using CleanTenant.ManagementApp.Themes;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// <see cref="IThemeService"/>'in Blazored.LocalStorage implementasyonu.
/// Tercih anahtarları: <c>cleantenant.theme.preset</c> ve <c>cleantenant.theme.dark</c>.
/// </summary>
public sealed class LocalStorageThemeService : IThemeService
{
    private const string PresetKey = "cleantenant.theme.preset";
    private const string DarkKey = "cleantenant.theme.dark";

    private readonly ILocalStorageService _storage;

    /// <summary>DI'dan storage'ı alır.</summary>
    public LocalStorageThemeService(ILocalStorageService storage)
    {
        _storage = storage;
    }

    /// <inheritdoc />
    public ThemePresetId CurrentPreset { get; private set; } = ThemePresetId.KurumsalMavi;

    /// <inheritdoc />
    public bool IsDarkMode { get; private set; }

    /// <inheritdoc />
    public event Action? ThemeChanged;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (await _storage.ContainKeyAsync(PresetKey))
        {
            var stored = await _storage.GetItemAsync<int>(PresetKey);
            if (Enum.IsDefined(typeof(ThemePresetId), stored))
            {
                CurrentPreset = (ThemePresetId)stored;
            }
        }

        if (await _storage.ContainKeyAsync(DarkKey))
        {
            IsDarkMode = await _storage.GetItemAsync<bool>(DarkKey);
        }

        ThemeChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetPresetAsync(ThemePresetId preset)
    {
        CurrentPreset = preset;
        await _storage.SetItemAsync(PresetKey, (int)preset);
        ThemeChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _storage.SetItemAsync(DarkKey, IsDarkMode);
        ThemeChanged?.Invoke();
    }
}
