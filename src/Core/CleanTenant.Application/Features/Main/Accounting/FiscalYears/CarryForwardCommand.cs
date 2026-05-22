using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// Kapalı mali yıldan yeni mali yıla bakiye devri yapar.
/// Bilanço hesaplarının (sınıf 1–5) kapanış bakiyeleri açılış fişi
/// olarak yeni mali yılın ilk dönemine yazılır.
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record CarryForwardCommand(
    Guid CompanyId,
    Guid TenantId,
    Guid FiscalYearId) : IRequest<Result<Guid>>;
