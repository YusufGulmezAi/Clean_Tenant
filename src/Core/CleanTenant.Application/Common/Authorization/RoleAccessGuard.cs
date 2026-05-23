using CleanTenant.Application.Common.Auth;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// <para>
/// Rol yönetimi handler'ları için ortak yetki kontrolleri (v0.2.8.c).
/// Privilege escalation, scope ceiling, sahiplik (TenantId/CompanyId) ve
/// built-in koruması burada konsolide edilir.
/// </para>
/// <para>
/// Hatalar <see cref="UnauthorizedAccessException"/> ile fırlatılır;
/// UI tarafı bu mesajı Snackbar'da kullanıcıya gösterir.
/// </para>
/// </summary>
public static class RoleAccessGuard
{
    /// <summary>
    /// Aktif kullanıcı (assigner) bu rolü düzenleyebilir/silebilir mi?
    /// Built-in koruması + global/tenant sahiplik kuralları uygulanır.
    /// </summary>
    public static void EnsureCanManageRole(AuthSession? session, Role role)
    {
        if (session is null)
            throw new UnauthorizedAccessException("Oturum bulunamadı.");

        // System tüm rolleri (built-in dahil) yönetebilir.
        if (session.ScopeLevel == ScopeLevel.System) return;

        // Rol/izin yönetimi yalnız Sistem ve Yönetim (Tenant) seviyesinde yapılabilir;
        // Site (Company) ve Birim (Unit) kullanıcıları rol/izin tanımlayamaz/düzenleyemez.
        if (session.ScopeLevel != ScopeLevel.Tenant)
            throw new UnauthorizedAccessException(
                "Rol ve izin yönetimi yalnız Sistem ve Yönetim seviyesinde yapılabilir.");

        if (role.IsBuiltIn)
            throw new UnauthorizedAccessException("Built-in roller yalnız Sistem tarafından düzenlenebilir.");

        if (role.TenantId is null)
            throw new UnauthorizedAccessException("Global roller yalnız Sistem tarafından yönetilebilir.");

        if (role.TenantId != session.TenantId)
            throw new UnauthorizedAccessException("Bu rolü yönetme yetkiniz yok (farklı yönetim).");

        // Yönetim (Tenant) kullanıcısı kendi tenant'ının tüm rollerini yönetir —
        // tenant-geneli + Site (Company) özel roller dahil.
    }

    /// <summary>
    /// Yeni rol oluşturmada — assigner kendi scope'undan daha geniş (System'e
    /// daha yakın) bir scope için rol oluşturamaz. System her scope'a oluşturabilir.
    /// </summary>
    public static void EnsureCanCreateAtScope(AuthSession? session, ScopeLevel newRoleScope)
    {
        if (session is null)
            throw new UnauthorizedAccessException("Oturum bulunamadı.");

        if (session.ScopeLevel == ScopeLevel.System) return;

        // Rol/izin tanımlama yalnız Sistem ve Yönetim (Tenant) seviyesinde yapılabilir;
        // Site (Company) ve Birim (Unit) kullanıcıları rol oluşturamaz.
        if (session.ScopeLevel != ScopeLevel.Tenant)
            throw new UnauthorizedAccessException(
                "Rol tanımlama yalnız Sistem ve Yönetim seviyesinde yapılabilir.");

        // newRole.Scope >= assigner.Scope (numerik) → narrower veya eşit yetki
        if ((int)newRoleScope < (int)session.ScopeLevel)
            throw new UnauthorizedAccessException(
                $"Sahip olduğunuz scope ({session.ScopeLevel}) bu seviyede rol oluşturamaz ({newRoleScope}).");
    }

    /// <summary>
    /// <para>
    /// <b>Privilege ceiling kontrolü.</b> Atanmak istenen izinlerin tamamı
    /// assigner'ın kendi izin setinde olmalı. System scope bypass eder.
    /// </para>
    /// <para>
    /// Bu kontrol kritik güvenlik kontrolüdür — UI tarafında izin filter'ı
    /// olsa bile API doğrudan çağrılabileceği için backend mecburidir.
    /// </para>
    /// </summary>
    public static void EnsurePermissionCeiling(
        AuthSession session,
        IReadOnlyCollection<string> requestedPermissionCodes)
    {
        if (session.ScopeLevel == ScopeLevel.System) return;

        var assignerCodes = session.Permissions.ToHashSet(StringComparer.Ordinal);
        var notOwned = requestedPermissionCodes
            .Where(c => !assignerCodes.Contains(c))
            .ToList();

        if (notOwned.Count > 0)
            throw new UnauthorizedAccessException(
                $"Sahip olmadığınız izinleri atayamazsınız: {string.Join(", ", notOwned)}.");
    }

    /// <summary>
    /// <para>
    /// <b>Scope ceiling kontrolü.</b> Atanan her iznin <c>MinimumRoleScope</c>'u,
    /// rolün scope'undan dar (numerik olarak büyük) ya da eşit olmalı. Yani
    /// Tenant scope rolüne System-only bir izin atanamaz.
    /// </para>
    /// </summary>
    public static void EnsureScopeCeiling(
        ScopeLevel roleScope,
        IReadOnlyCollection<Permission> requestedPermissions)
    {
        // None (0) = "scope kısıtlaması yok" → seeder güncellemesi henüz uygulanmamış
        // permission'lar için skip; tek yetki kontrolü EnsurePermissionCeiling olur.
        var violations = requestedPermissions
            .Where(p => p.MinimumRoleScope != ScopeLevel.None
                        && (int)roleScope > (int)p.MinimumRoleScope)
            .Select(p => $"{p.Code} (min: {p.MinimumRoleScope})")
            .ToList();

        if (violations.Count > 0)
            throw new InvalidOperationException(
                $"Bu izinler {roleScope} scope rolüne atanamaz: {string.Join(", ", violations)}.");
    }
}
