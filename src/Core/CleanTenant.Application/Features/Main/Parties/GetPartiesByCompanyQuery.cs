using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Parties.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary>
/// Şirketin cari kişilerini (opsiyonel ad/TCKN/telefon araması) listeler.
/// Telefon maskelenir; arama ham veride çalışır (yetki gözetmeksizin eşleşme).
/// </summary>
[RequirePermission("tenant.party.view")]
public sealed record GetPartiesByCompanyQuery(
    Guid CompanyId,
    string? Search = null,
    int Take = 50) : IRequest<Result<IReadOnlyList<PartyListItem>>>;

/// <summary>Cari kişi liste öğesi (PII maskeli olabilir).</summary>
public sealed record PartyListItem(
    Guid Id,
    string UrlCode,
    PartyKind Kind,
    string FullName,
    string? Tckn,
    string? Phone,
    string? Email);
