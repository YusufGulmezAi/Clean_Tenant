using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kullanıcı onboarding sırasında gerçek zamanlı kullanıcı araması.
/// TCKN/VKN/YKN, telefon veya e-posta ile arama yapar.
/// Bulunan kullanıcı için özet bilgi döner; bulunamazsa null döner (hata değil).
/// </summary>
[RequirePermission("System.Users.Manage", "Tenant.Users.Manage")]
public sealed record LookupUserByIdentifierQuery(
    UserLookupType Type,
    string Value) : IRequest<Result<UserLookupResult?>>;

/// <summary>Kullanıcı arama tipi.</summary>
public enum UserLookupType
{
    /// <summary>TC Kimlik / Yabancı Kimlik Numarası (11 hane).</summary>
    Tckn,

    /// <summary>Vergi Kimlik Numarası (10 hane).</summary>
    Vkn,

    /// <summary>Cep telefonu numarası (Türkiye formatı).</summary>
    Phone,

    /// <summary>E-posta adresi.</summary>
    Email,
}

/// <summary>Kullanıcı arama sonucu. Yalnızca onboarding akışında görüntülenmek üzere yeterli bilgi içerir.</summary>
public sealed record UserLookupResult(
    Guid Id,
    string UrlCode,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive);
