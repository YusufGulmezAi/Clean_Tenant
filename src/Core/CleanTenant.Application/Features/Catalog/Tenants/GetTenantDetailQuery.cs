using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// Bir Yönetim'in tam detayını döner — Edit formu, Settings sayfası için.
/// <see cref="CacheableAttribute"/> ile MediatR CachingBehavior pipeline cache'ler;
/// TTL 10 dk. Tenant CRUD'ında <c>ICacheInvalidator.InvalidateTenantAsync</c>
/// cascade siler.
/// </para>
/// <para>
/// Yetki: <c>Tenant.Read</c>; AuthorizationBehavior pipeline kontrol eder.
/// </para>
/// </summary>
[RequirePermission("Tenant.Read")]
[Cacheable("catalog:tenants:detail:{TenantId}", CacheTtlPreset.DetailMediumLived)]
public sealed record GetTenantDetailQuery(Guid TenantId) : IRequest<Result<TenantDetail>>;
