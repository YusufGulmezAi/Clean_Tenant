using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>Mali yıl liste elemanı.</summary>
public record FiscalYearListItem(
    Guid Id,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    PeriodStatus Status,
    bool IsCurrentYear,
    int PeriodCount);

/// <summary>Mali yıl tam detay — dönemler dahil.</summary>
public record FiscalYearDetail(
    Guid Id,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    PeriodStatus Status,
    bool IsCurrentYear,
    IReadOnlyList<PeriodSummary> Periods);

/// <summary>Mali yıl detayında dönem özeti.</summary>
public record PeriodSummary(
    Guid Id,
    int Year,
    int Month,
    DateOnly StartDate,
    DateOnly EndDate,
    PeriodStatus Status);
