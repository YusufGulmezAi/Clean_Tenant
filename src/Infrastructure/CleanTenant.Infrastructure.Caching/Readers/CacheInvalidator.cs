using CleanTenant.Application.Common.Caching;

namespace CleanTenant.Infrastructure.Caching.Readers;

/// <summary>
/// <para>
/// <see cref="ICacheInvalidator"/>'ın <see cref="ICacheStore"/> üzerinden çalışan
/// implementasyonu. Domain-aware invalidation:
/// </para>
/// <list type="bullet">
///   <item>Tenant değişimi → <c>by-id</c> + <c>by-url-code</c> + <c>all-active</c> liste cache'i sil.</item>
///   <item>Company değişimi → <c>by-id</c> + <c>by-url-code</c> + parent tenant'ın
///     <c>by-tenant</c> listesi + global <c>all-global</c> listesi sil.</item>
/// </list>
/// </summary>
public sealed class CacheInvalidator : ICacheInvalidator
{
    private readonly ICacheStore _cache;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CacheInvalidator(ICacheStore cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task InvalidateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Tenant.ById(tenantId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Tenant.DetailById(tenantId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Tenant.AllActive, cancellationToken);
        // UrlCode by-id'den bilinmediği için prefix temizliği yeterli
        await _cache.RemoveByPrefixAsync($"{CacheKeys.Tenant.Prefix}:by-url-code:", cancellationToken);
        // MediatR CachingBehavior key (GetTenantDetailQuery — KeyTemplate "catalog:tenants:detail:{TenantId}")
        await _cache.RemoveAsync($"{CacheKeys.KeyPrefix}:mediatr:catalog:tenants:detail:{tenantId:N}", cancellationToken);
        // Company listelerinde TenantName denormalize olduğu için global liste de temizlenmeli
        await _cache.RemoveAsync(CacheKeys.Company.AllGlobal, cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateCompanyAsync(Guid companyId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Company.ById(companyId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Company.ByTenant(tenantId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Company.AllGlobal, cancellationToken);
        await _cache.RemoveByPrefixAsync($"{CacheKeys.Company.Prefix}:by-url-code:", cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateAllTenantsAsync(CancellationToken cancellationToken = default)
        => _cache.RemoveByPrefixAsync(CacheKeys.Tenant.Prefix, cancellationToken);

    /// <inheritdoc />
    public Task InvalidateAllCompaniesAsync(CancellationToken cancellationToken = default)
        => _cache.RemoveByPrefixAsync(CacheKeys.Company.Prefix, cancellationToken);

    /// <inheritdoc />
    public async Task InvalidateRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Role.ById(roleId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Role.DetailById(roleId), cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Authorization.PermissionsByRole(roleId), cancellationToken);
        // Scope-based list cache'i sil (scope level bilinmediği için prefix-based yeterli değil,
        // tüm scope cache'leri temizle)
        await _cache.RemoveByPrefixAsync(CacheKeys.Role.Prefix, cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateAllRolesAsync(CancellationToken cancellationToken = default)
        => _cache.RemoveByPrefixAsync(CacheKeys.Role.Prefix, cancellationToken);

    /// <inheritdoc />
    public async Task InvalidatePermissionsAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.Permission.All, cancellationToken);
        // Permission update → tüm role detail'ler de potentially etkilenir (PermissionIds denormalize)
        await _cache.RemoveByPrefixAsync(CacheKeys.Role.Prefix, cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Authorization.Prefix, cancellationToken);
    }

    /// <inheritdoc />
    public Task InvalidateAllUserContextsAsync(CancellationToken cancellationToken = default)
        => _cache.RemoveByPrefixAsync(CacheKeys.User.Prefix, cancellationToken);
}
