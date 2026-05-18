using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

public sealed record GetDistrictsByProvinceQuery(Guid ProvinceId) : IRequest<Result<IReadOnlyList<DistrictListItem>>>;
