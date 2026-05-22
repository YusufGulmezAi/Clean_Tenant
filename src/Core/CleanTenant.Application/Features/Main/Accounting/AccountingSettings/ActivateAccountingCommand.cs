using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// Şirket için muhasebe modülünü aktifleştirir.
/// <para>
/// İşlem sırası:
/// 1. AccountingSettings yoksa oluşturur.
/// 2. TDHP hesap planını başlatır (InitializeChartOfAccountsCommand).
/// 3. Cari takvim yılı için varsayılan FiscalYear oluşturur.
/// </para>
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record ActivateAccountingCommand(
    Guid CompanyId,
    Guid TenantId) : IRequest<Result<AccountingSettingsDto>>;
