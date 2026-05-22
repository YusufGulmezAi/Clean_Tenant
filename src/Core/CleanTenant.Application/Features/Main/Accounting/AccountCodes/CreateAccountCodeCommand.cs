using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// Şirkete özgü yeni bir hesap kodu oluşturur (Source = Custom).
/// </summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record CreateAccountCodeCommand(
    Guid CompanyId,
    Guid TenantId,
    string Code,
    string? ParentCode,
    string Name,
    string? Description,
    AccountLevel Level,
    AccountClass AccountClass,
    AccountType AccountType,
    bool IsDetail,
    bool IsMonetary,
    DateOnly? AcquisitionDate) : IRequest<Result<AccountCodeDetail>>;
