using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>Şirketin muhasebe yapılandırmasını döner.</summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record GetAccountingSettingsQuery(
    Guid CompanyId) : IRequest<Result<AccountingSettingsDto>>;
