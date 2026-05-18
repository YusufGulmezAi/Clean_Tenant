using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// Site'yi sil (soft delete — IsDeleted flag'i set edilir).
/// </summary>
public sealed record DeleteCompanyCommand(Guid CompanyId) : IRequest<Result<Unit>>;
