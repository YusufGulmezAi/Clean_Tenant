using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Jobs;
using CleanTenant.Infrastructure.Identity.Context;
using CleanTenant.SharedKernel.Context;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.Identity.Jobs;

/// <summary>
/// <see cref="ISystemJobExecutor"/> implementasyonu. Yeni bir DI scope açar,
/// scope'taki <see cref="HttpUserContext"/>'e sentetik bir Tenant-scope sistem
/// oturumu yazar (böylece <c>ITenantContext</c> + query filter + permission check
/// + audit doğru çalışır) ve work'ü o scope'ta çalıştırır.
/// </summary>
internal sealed class SystemJobExecutor : ISystemJobExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SystemJobExecutor(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <inheritdoc />
    public async Task<T> RunForTenantAsync<T>(
        Guid tenantId,
        IReadOnlyList<string> permissions,
        Func<IServiceProvider, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        // Aynı scope'ta ITenantContext + ICurrentSessionAccessor bu instance'ı paylaşır.
        var userContext = scope.ServiceProvider.GetRequiredService<HttpUserContext>();
        userContext.Current = BuildSystemSession(tenantId, permissions);

        return await work(scope.ServiceProvider, cancellationToken);
    }

    private static AuthSession BuildSystemSession(Guid tenantId, IReadOnlyList<string> permissions) => new()
    {
        SessionId = Guid.Empty,
        UserId = SystemActor.UserId,
        ContextId = Guid.Empty,
        Email = "system@cleantenant.local",
        UserName = "system",
        ScopeLevel = ScopeLevel.Tenant,
        TenantId = tenantId,
        Roles = [],
        Permissions = permissions,
        PersonaSide = PersonaSide.Management,
        IsSystemSession = true,
        IssuedAt = DateTimeOffset.UtcNow,
    };
}
