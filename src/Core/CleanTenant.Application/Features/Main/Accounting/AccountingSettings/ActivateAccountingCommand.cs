using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>
/// Şirket için muhasebe modülünü aktifleştirir.
/// <para>
/// İşlem sırası:
/// 1. AccountingSettings yoksa oluşturur.
/// 2. TDHP hesap planını başlatır (InitializeChartOfAccountsCommand).
/// 3. İlk FiscalYear'ı oluşturur — <see cref="FiscalYearStart"/>/<see cref="FiscalYearEnd"/>
///    verilmişse o aralık (sitenin kendi dönemi), verilmemişse cari takvim yılı (geriye uyum).
/// </para>
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record ActivateAccountingCommand(
    Guid CompanyId,
    Guid TenantId,
    string? FiscalYearLabel = null,
    DateOnly? FiscalYearStart = null,
    DateOnly? FiscalYearEnd = null) : IRequest<Result<AccountingSettingsDto>>;
