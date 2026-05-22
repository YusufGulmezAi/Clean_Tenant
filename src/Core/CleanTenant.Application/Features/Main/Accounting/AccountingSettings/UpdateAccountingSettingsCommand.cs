using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>Muhasebe yapılandırmasını günceller.</summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record UpdateAccountingSettingsCommand(
    Guid CompanyId,
    bool RequireApproval,
    string DefaultCurrency,
    VatPeriod VatPeriod,
    bool EDefterEnabled) : IRequest<Result<AccountingSettingsDto>>;
