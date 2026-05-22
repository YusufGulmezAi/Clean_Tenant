using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.Periods;

/// <summary>Muhasebe dönemi liste elemanı.</summary>
public record PeriodListItem(
    Guid Id,
    Guid FiscalYearId,
    int Year,
    int Month,
    DateOnly StartDate,
    DateOnly EndDate,
    PeriodStatus Status);
