using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

public sealed record GetNeighborhoodsByDistrictQuery(Guid DistrictId) : IRequest<Result<IReadOnlyList<NeighborhoodListItem>>>;
