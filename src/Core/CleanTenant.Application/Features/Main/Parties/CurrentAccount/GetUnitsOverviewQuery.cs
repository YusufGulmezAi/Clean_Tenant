using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary>Şirketin tüm BB'lerini borç özetiyle listeler (Cari Kart BB seçimi / genel bakış).</summary>
[RequirePermission("tenant.currentaccount.view")]
public sealed record GetUnitsOverviewQuery(
    Guid CompanyId) : IRequest<Result<IReadOnlyList<UnitOverviewRow>>>;
