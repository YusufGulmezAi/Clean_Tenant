using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

public sealed record GetResidentialTypesQuery : IRequest<Result<IReadOnlyList<ResidentialTypeListItem>>>;
