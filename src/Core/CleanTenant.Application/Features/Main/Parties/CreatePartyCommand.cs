using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Parties.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary>
/// Yeni cari kişi (Party) oluşturur. Birey ise TCKN, tüzel ise VKN beklenir
/// (zorunlu değil; eksik veri girişine izin verilir). (CompanyId, Tckn) tekildir.
/// </summary>
[RequirePermission("tenant.party.edit")]
public sealed record CreatePartyCommand(
    Guid TenantId,
    Guid CompanyId,
    PartyKind Kind,
    string FullName,
    string? FirstName = null,
    string? LastName = null,
    string? TradeName = null,
    string? Tckn = null,
    string? Vkn = null,
    DateOnly? BirthDate = null,
    string? Phone = null,
    string? Email = null,
    string? AddressLine = null,
    string? Notes = null,
    string? TagsJson = null,
    bool KvkkConsentGiven = false,
    DateTimeOffset? KvkkConsentAt = null,
    string? KvkkConsentChannel = null) : IRequest<Result<Guid>>;
