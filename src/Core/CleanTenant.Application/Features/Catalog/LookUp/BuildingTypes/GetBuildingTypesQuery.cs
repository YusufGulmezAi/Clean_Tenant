using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

public sealed record GetBuildingTypesQuery : IRequest<Result<IReadOnlyList<BuildingTypeListItem>>>;
