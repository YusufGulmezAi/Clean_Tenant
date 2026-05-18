using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

[RequirePermission("LookUp.Manage")]
public sealed record CreateBankCommand(string FullName, string ShortName, string? EftCode = null, bool HasVirtualPosIntegration = false, bool HasCorporateCollectionIntegration = false) : IRequest<Result<Guid>>;
