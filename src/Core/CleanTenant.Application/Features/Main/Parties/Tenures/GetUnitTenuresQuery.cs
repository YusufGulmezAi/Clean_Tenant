using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Parties.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>
/// Bir Bağımsız Bölümün tenure ağacını getirir: Malikler (aktif→ilk),
/// Kiracılar (son→ilk), İletişim kişileri. Cari Kart sol panelini besler.
/// </summary>
[RequirePermission("tenant.party.view")]
public sealed record GetUnitTenuresQuery(
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<UnitTenures>>;

/// <summary>BB tenure ağacı kökü.</summary>
public sealed record UnitTenures(
    IReadOnlyList<OwnershipItem> Owners,
    IReadOnlyList<TenancyItem> Tenants,
    IReadOnlyList<ContactItem> Contacts);

/// <summary>Malik tenure satırı (pay% + müteselsil).</summary>
public sealed record OwnershipItem(
    Guid Id,
    Guid PartyId,
    string PartyName,
    string PartyUrlCode,
    decimal SharePercent,
    bool IsJointAndSeveral,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes);

/// <summary>Kiracı tenure satırı.</summary>
public sealed record TenancyItem(
    Guid Id,
    Guid PartyId,
    string PartyName,
    string PartyUrlCode,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes);

/// <summary>İletişim kişisi tenure satırı.</summary>
public sealed record ContactItem(
    Guid Id,
    Guid PartyId,
    string PartyName,
    string PartyUrlCode,
    ContactRole ContactRole,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes);
