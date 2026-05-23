using CleanTenant.Application.Features.Profile.GetTheme;
using CleanTenant.Application.Features.Profile.SetTheme;
using CleanTenant.ManagementApp.Themes;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// <para>
/// <see cref="IThemeService"/>'in DB-kaynaklı implementasyonu (v0.2.13.d).
/// Kullanıcının tema tercihi (renk preset adı + gece modu) <c>User</c> tablosunda
/// saklanır; MediatR ile okunur (<see cref="GetUserThemeQuery"/>) ve yazılır
/// (<see cref="SetUserThemeCommand"/>).
/// </para>
/// <para>
/// Önceki <c>LocalStorageThemeService</c>'in yerini alır: tema artık tarayıcıya
/// değil hesaba bağlıdır — cihazlar arası taşınır ve her login'de (yeni Blazor
/// circuit → <see cref="InitializeAsync"/>) otomatik uygulanır. Preset değeri
/// <see cref="ThemePresetId"/> enum adı olarak saklanır; null → "Kurumsal Mavi".
/// </para>
/// </summary>
public sealed class UserThemeService : IThemeService
{
    private readonly ISender _sender;

    /// <summary>DI'dan MediatR sender'ı alır.</summary>
    public UserThemeService(ISender sender) => _sender = sender;

    /// <inheritdoc />
    public ThemePresetId CurrentPreset { get; private set; } = ThemePresetId.KurumsalMavi;

    /// <inheritdoc />
    public bool IsDarkMode { get; private set; }

    /// <inheritdoc />
    public event Action? ThemeChanged;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Kök layout'tan çağrılır; tema yüklemesi başarısız olursa UI çökmemeli —
        // varsayılan (Kurumsal Mavi, açık mod) ile devam et.
        try
        {
            var result = await _sender.Send(new GetUserThemeQuery());
            if (result.IsSuccess && result.Value is not null)
            {
                CurrentPreset = ParsePreset(result.Value.Preset);
                IsDarkMode = result.Value.DarkMode;
            }
        }
        catch
        {
            CurrentPreset = ThemePresetId.KurumsalMavi;
            IsDarkMode = false;
        }

        ThemeChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task SetPresetAsync(ThemePresetId preset)
    {
        if (preset == CurrentPreset) return;

        CurrentPreset = preset;
        ThemeChanged?.Invoke();   // Anında UI güncellemesi (DB yazımını beklemeden)
        await PersistAsync();
    }

    /// <inheritdoc />
    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        ThemeChanged?.Invoke();
        await PersistAsync();
    }

    /// <summary>Mevcut tercih durumunu DB'ye yazar.</summary>
    private Task<Result> PersistAsync()
        => _sender.Send(new SetUserThemeCommand(CurrentPreset.ToString(), IsDarkMode));

    /// <summary>Saklanan preset adını enum'a çevirir; tanımsız/null ise varsayılan.</summary>
    private static ThemePresetId ParsePreset(string? name)
        => Enum.TryParse<ThemePresetId>(name, ignoreCase: true, out var id) && Enum.IsDefined(id)
            ? id
            : ThemePresetId.KurumsalMavi;
}
