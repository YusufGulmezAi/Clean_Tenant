using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary>Bir şirketin belirli muhasebe dönemindeki tüm tahsilatlarını listeler.</summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetCollectionsByPeriodQuery(
    Guid CompanyId,
    Guid AccountingPeriodId) : IRequest<Result<IReadOnlyList<CollectionListItem>>>;
