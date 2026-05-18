using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

[RequirePermission("LookUp.Manage")]
public sealed record DeleteProvinceCommand(Guid Id) : IRequest<Result>;
