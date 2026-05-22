using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// Hesap kodunu pasifleştirir. IsRequired=true olan zorunlu hesaplar deaktive edilemez.
/// Aktif fiş satırı bulunan hesaplar pasifleştirilemez.
/// </summary>
[RequirePermission("company.accounting.account-plan.write")]
public sealed record DeactivateAccountCodeCommand(
    Guid CompanyId,
    Guid AccountCodeId) : IRequest<Result<bool>>;
