using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <para>
/// Belirli bir Yönetim (tenant) bağlamı içinde yeni Site oluştur.
/// UrlCode otomatik üretilir (Base58, 9 karakter).
/// </para>
/// <para>
/// v0.2.13.e — Her yeni Site açılışında zorunlu bir <b>CompanyAdmin</b> (süper company
/// kullanıcısı) provision edilir: <c>Admin*</c> alanları sorumlu site yöneticisini
/// tanımlar (gerçek kişi). E-posta zaten kayıtlıysa mevcut kullanıcı yeniden kullanılır.
/// </para>
/// </summary>
/// <param name="TenantId">Sitenin bağlı olduğu Yönetim.</param>
/// <param name="Name">Site adı (zorunlu, max 256).</param>
/// <param name="LegalName">Yasal ad (opsiyonel, max 512).</param>
/// <param name="Vkn">Vergi kimlik numarası (opsiyonel, 10 hane).</param>
/// <param name="Email">Site iletişim e-postası (opsiyonel).</param>
/// <param name="Phone">Site iletişim telefonu (opsiyonel).</param>
/// <param name="AdminFirstName">Site yöneticisi adı.</param>
/// <param name="AdminLastName">Site yöneticisi soyadı.</param>
/// <param name="AdminEmail">Site yöneticisi e-postası (CompanyAdmin olarak atanır).</param>
/// <param name="AdminPhone">Site yöneticisi telefonu (opsiyonel).</param>
[RequirePermission("Company.Create")]
[TenantWriteOperation]
public sealed record CreateCompanyCommand(
    Guid TenantId,
    string Name,
    string? LegalName,
    string? Vkn,
    string? Email,
    string? Phone,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string? AdminPhone) : IRequest<Result<CompanyDetail>>;
