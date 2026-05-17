using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <para>
/// Bir kullanıcının erişebileceği scope seçenekleri arasından "primary scope"
/// seçen ve persona'ya göre filter uygulayan yardımcı.
/// </para>
/// </summary>
public static class ScopeSelector
{
    /// <summary>
    /// Verilen scope listesini persona'ya göre filtreler.
    /// <list type="bullet">
    ///   <item><see cref="PersonaSide.Management"/> → System / Tenant / Company.</item>
    ///   <item><see cref="PersonaSide.Portal"/> → Unit.</item>
    /// </list>
    /// </summary>
    public static IEnumerable<ScopeOption> FilterByPersona(
        IEnumerable<ScopeOption> options,
        PersonaSide persona)
    {
        return persona switch
        {
            PersonaSide.Management => options.Where(o =>
                o.Level is ScopeLevel.System or ScopeLevel.Tenant or ScopeLevel.Company),
            PersonaSide.Portal => options.Where(o => o.Level == ScopeLevel.Unit),
            _ => [],
        };
    }

    /// <summary>
    /// Persona'nın izin verdiği scope'lardan primary'i seçer.
    /// <list type="bullet">
    ///   <item><b>Management:</b> System > Tenant > Company; eşitlik durumunda tenant/company adına göre alfabetik.</item>
    ///   <item><b>Portal:</b> İlk Unit (genelde tek seçenek).</item>
    /// </list>
    /// Boş liste null döner; çağıran "erişilebilir scope yok" hatasını işler.
    /// </summary>
    public static ScopeOption? SelectPrimary(
        IReadOnlyList<ScopeOption> filteredOptions,
        PersonaSide persona)
    {
        if (filteredOptions.Count == 0)
        {
            return null;
        }

        return persona switch
        {
            PersonaSide.Management => filteredOptions
                .OrderBy(o => ScopePriority(o.Level))
                .ThenBy(o => o.TenantName ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(o => o.CompanyName ?? string.Empty, StringComparer.Ordinal)
                .First(),
            PersonaSide.Portal => filteredOptions
                .OrderBy(o => o.UnitLabel ?? string.Empty, StringComparer.Ordinal)
                .First(),
            _ => null,
        };
    }

    /// <summary>System önce, sonra Tenant, sonra Company.</summary>
    private static int ScopePriority(ScopeLevel level) => level switch
    {
        ScopeLevel.System => 0,
        ScopeLevel.Tenant => 1,
        ScopeLevel.Company => 2,
        ScopeLevel.Unit => 3,
        _ => 99,
    };
}
