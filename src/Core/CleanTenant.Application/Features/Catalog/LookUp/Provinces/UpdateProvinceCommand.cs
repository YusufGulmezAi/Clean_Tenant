using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

[RequirePermission("LookUp.Manage")]
public sealed record UpdateProvinceCommand(Guid Id, string Name, int? PlateCode) : IRequest<Result>;
