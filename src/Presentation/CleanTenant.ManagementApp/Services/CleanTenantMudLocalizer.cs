using Microsoft.Extensions.Localization;
using MudBlazor;

namespace CleanTenant.ManagementApp.Services;

/// <summary>
/// <para>
/// MudBlazor'ın default İngilizce resource'larını DB-tabanlı
/// <see cref="IStringLocalizer"/> üzerinden 5 dilli (TR/EN/AR/RU/DE) çevirilere
/// bağlar. Anahtarlar <c>LocalizationCatalog</c>'da <c>MudDataGrid.*</c>,
/// <c>MudTable.*</c>, <c>MudDialog.*</c> vb. olarak seed edilir.
/// </para>
/// <para>
/// Anahtar DB'de yoksa <c>resourceNotFound: true</c> dönülür ve MudBlazor
/// kendi İngilizce fallback'ine düşer — yeni MudBlazor sürümleriyle gelen
/// ek anahtarlar projeyi kırmaz.
/// </para>
/// <para>
/// Program.cs'de <c>AddMudServices()</c>'den ÖNCE kayıt edilmeli; aksi
/// takdirde MudBlazor'ın <c>TryAddTransient&lt;MudLocalizer&gt;</c>'i bizimkini
/// geçersiz kılabilir.
/// </para>
/// </summary>
public sealed class CleanTenantMudLocalizer : MudLocalizer
{
    private readonly IStringLocalizer _localizer;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CleanTenantMudLocalizer(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <inheritdoc />
    public override LocalizedString this[string key]
    {
        get
        {
            var resolved = _localizer[key];
            if (!resolved.ResourceNotFound)
            {
                return new LocalizedString(key, resolved.Value, resourceNotFound: false);
            }

            // DB'de yoksa MudBlazor default'una (İngilizce) düş.
            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}
