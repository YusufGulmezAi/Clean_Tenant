using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// Mevcut hesap kodunu günceller. IsRequired=true olan standart hesapların
/// kodu değiştirilemez; yalnızca adı ve açıklaması güncellenebilir.
/// </summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record UpdateAccountCodeCommand(
    Guid CompanyId,
    Guid AccountCodeId,
    string Name,
    string? Description,
    bool IsMonetary,
    bool IsActive,
    bool IsDetail) : IRequest<Result<AccountCodeDetail>>;
