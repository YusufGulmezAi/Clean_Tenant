namespace CleanTenant.Application.Common.Jobs;

/// <summary>
/// Arka plan job'larının (Hangfire) bir tenant bağlamında çalışmasını sağlar.
/// HTTP isteği/oturumu olmadığından, verilen tenant için sentetik bir sistem
/// oturumu kurar (Tenant scope + belirtilen izinler), yeni bir DI scope açar ve
/// verilen işi o scope'ta çalıştırır. Böylece global query filter tenant'a göre
/// izole olur ve MediatR pipeline auth/audit doğru akar.
/// </summary>
public interface ISystemJobExecutor
{
    /// <summary>
    /// <paramref name="tenantId"/> için sentetik sistem oturumu kurup yeni scope'ta
    /// <paramref name="work"/>'ü çalıştırır. <paramref name="permissions"/> oturuma
    /// verilecek izin kodlarıdır (örn. <c>tenant.accrual.generate</c>).
    /// </summary>
    Task<T> RunForTenantAsync<T>(
        Guid tenantId,
        IReadOnlyList<string> permissions,
        Func<IServiceProvider, CancellationToken, Task<T>> work,
        CancellationToken cancellationToken);
}
