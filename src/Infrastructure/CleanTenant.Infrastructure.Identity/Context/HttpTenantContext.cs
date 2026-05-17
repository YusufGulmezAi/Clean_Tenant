using CleanTenant.Infrastructure.Identity.Context;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Identity.Context;

/// <summary>
/// HTTP scope'undaki <see cref="ITenantContext"/>. SessionLookupMiddleware'in
/// yüklediği <see cref="HttpUserContext.Current"/> session'ından scope
/// bilgilerini çıkarır.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private readonly HttpUserContext _userContext;

    /// <summary>Aynı scope'taki HttpUserContext'ten session'ı paylaşır.</summary>
    public HttpTenantContext(HttpUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <inheritdoc />
    public Guid? TenantId => _userContext.Current?.TenantId;

    /// <inheritdoc />
    public Guid? CompanyId => _userContext.Current?.CompanyId;

    /// <inheritdoc />
    public Guid? UnitId => _userContext.Current?.UnitId;

    /// <inheritdoc />
    public ScopeLevel CurrentScope => _userContext.Current?.ScopeLevel ?? ScopeLevel.None;
}
