using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Accounting.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Reports;

/// <summary>
/// Maliyet merkezi raporunu getirir — merkez ve hesap bazlı gider dağılımı.
/// <para>
/// <paramref name="CostCenterId"/> null ise tüm maliyet merkezleri raporlanır.
/// </para>
/// </summary>
[RequirePermission("company.accounting.reports.read")]
public sealed record GetCostCenterReportQuery(
    Guid CompanyId,
    Guid? CostCenterId,
    DateOnly From,
    DateOnly To) : IRequest<Result<IReadOnlyList<CostCenterReportEntry>>>;
