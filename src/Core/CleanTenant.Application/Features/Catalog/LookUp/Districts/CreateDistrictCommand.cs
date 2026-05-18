using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

[RequirePermission("LookUp.Manage")]
public sealed record CreateDistrictCommand(string Name, Guid ProvinceId) : IRequest<Result<Guid>>;
