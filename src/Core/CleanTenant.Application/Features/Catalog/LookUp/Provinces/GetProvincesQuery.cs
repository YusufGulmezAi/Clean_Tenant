using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

public sealed record GetProvincesQuery : IRequest<Result<IReadOnlyList<ProvinceListItem>>>;
