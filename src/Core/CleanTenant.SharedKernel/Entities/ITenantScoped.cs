namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// Bir tenant'a ait olan (tenant-scoped) entity'lerin arabirimidir.
/// </para>
/// <para>
/// EF Core global query filter'ı bu arabirimi implement eden entity'ler için
/// otomatik olarak <c>e =&gt; e.TenantId == _tenantContext.TenantId</c>
/// uygular; bir tenant'ın verisi başka tenant'a sızmaz (shared DB modu).
/// </para>
/// <para>
/// Tenant'tan bağımsız (global) entity'ler bu arabirimi implement etmez:
/// <c>Tenant</c>'ın kendisi, sistem rolleri, lokalizasyon kaynakları,
/// global user kaydı (Catalog DB'de).
/// </para>
/// </summary>
public interface ITenantScoped
{
    /// <summary>Entity'nin ait olduğu tenant'ın kimliği.</summary>
    Guid TenantId { get; set; }
}
