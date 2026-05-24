using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.CurrentAccount;

/// <summary>Bir BB'nin cari hareket defterini (borç/alacak/bakiye) getirir.</summary>
[RequirePermission("tenant.currentaccount.view")]
public sealed record GetUnitLedgerQuery(
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<IReadOnlyList<LedgerEntryRow>>>;
