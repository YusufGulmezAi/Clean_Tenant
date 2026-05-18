using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

[RequirePermission("LookUp.Manage")]
public sealed record DeleteBankCommand(Guid Id) : IRequest<Result>;
