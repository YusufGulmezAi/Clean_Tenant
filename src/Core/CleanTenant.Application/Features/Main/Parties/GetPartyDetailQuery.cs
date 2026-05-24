using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Parties.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary>
/// Bir cari kişinin detayını getirir. PII (TCKN/VKN/telefon) yalnız
/// <c>tenant.party.pii.view</c> izniyle maskesiz döner.
/// </summary>
[RequirePermission("tenant.party.view")]
public sealed record GetPartyDetailQuery(
    Guid CompanyId,
    Guid PartyId) : IRequest<Result<PartyDetail>>;

/// <summary>Cari kişi detay DTO'su (PII maskeli olabilir).</summary>
public sealed record PartyDetail(
    Guid Id,
    string UrlCode,
    PartyKind Kind,
    string FullName,
    string? FirstName,
    string? LastName,
    string? TradeName,
    string? Tckn,
    string? Vkn,
    DateOnly? BirthDate,
    string? Phone,
    string? Email,
    string? AddressLine,
    string? Notes,
    string? TagsJson,
    bool KvkkConsentGiven,
    DateTimeOffset? KvkkConsentAt,
    string? KvkkConsentChannel,
    bool PiiMasked);
