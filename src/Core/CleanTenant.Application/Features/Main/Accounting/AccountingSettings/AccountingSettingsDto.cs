using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.AccountingSettings;

/// <summary>Şirket muhasebe yapılandırması DTO'su.</summary>
public record AccountingSettingsDto(
    Guid Id,
    Guid CompanyId,
    bool IsActivated,
    bool RequireApproval,
    string DefaultCurrency,
    VatPeriod VatPeriod,
    bool EDefterEnabled);
