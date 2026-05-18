using CleanTenant.Application.Features.Catalog.Readers;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// <see cref="GetTenantDetailQuery"/> handler — <see cref="ITenantCatalogReader"/>
/// üzerinden detay döner. Reader cache + DB fallback yapar; bu handler asıl
/// CachingBehavior pipeline'ı yararına yine cache'lenir (key-template farklı,
/// MediatR cache'i query-level; reader cache entity-level).
/// </para>
/// </summary>
public sealed class GetTenantDetailQueryHandler : IRequestHandler<GetTenantDetailQuery, Result<TenantDetail>>
{
    private readonly ITenantCatalogReader _reader;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetTenantDetailQueryHandler(ITenantCatalogReader reader)
    {
        _reader = reader;
    }

    /// <inheritdoc />
    public async Task<Result<TenantDetail>> Handle(GetTenantDetailQuery query, CancellationToken cancellationToken)
    {
        var detail = await _reader.GetDetailByIdAsync(query.TenantId, cancellationToken);
        if (detail is null)
        {
            return Result<TenantDetail>.Failure(Error.NotFound("TENANT-NOT-FOUND", "Yönetim bulunamadı."));
        }
        return Result<TenantDetail>.Success(detail);
    }
}
