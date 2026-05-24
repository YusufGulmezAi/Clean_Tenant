using CleanTenant.Domain.Auditing;
using CleanTenant.Domain.Tenant.Parties.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Tenant.Parties;

/// <summary>
/// <para>
/// Cari kişi — bir sitenin malik/kiracı/iletişim kişisi olabilen gerçek veya
/// tüzel kişi. Sistemin login kimliği olan <c>User</c>'dan AYRIDIR (sakinlerin
/// çoğu login kullanıcısı değildir). BB ile ilişki <see cref="UnitOwnership"/>,
/// <see cref="UnitTenancy"/>, <see cref="UnitContact"/> tenure kayıtları ile kurulur.
/// </para>
/// <para>
/// PII alanları (<see cref="Tckn"/>, <see cref="Vkn"/>, <see cref="Phone"/>)
/// <see cref="SensitiveAttribute"/> ile işaretlidir; audit delta'sında maskelenir.
/// UI maskeleme yetki (<c>tenant.party.pii.view</c>) bazlı ayrıca yapılır.
/// </para>
/// </summary>
public sealed class Party : BaseEntity, IAggregateRoot, ITenantScoped, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL/paylaşım kodu (ShortCode). Interceptor doldurur.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Multi-tenancy izolasyon filtresi.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Verinin ait olduğu site (Company). Cari kart Company-scoped çalışır.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Gerçek/tüzel kişi ayrımı.</summary>
    public PartyKind Kind { get; set; }

    /// <summary>Görünen tam ad (bireyde ad+soyad, tüzelde ticari unvan).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Ad (yalnız birey).</summary>
    public string? FirstName { get; set; }

    /// <summary>Soyad (yalnız birey).</summary>
    public string? LastName { get; set; }

    /// <summary>Ticari unvan (yalnız tüzel).</summary>
    public string? TradeName { get; set; }

    /// <summary>TC Kimlik No (yalnız birey). PII.</summary>
    [Sensitive]
    public string? Tckn { get; set; }

    /// <summary>Vergi Kimlik No (tüzel veya bireysel vergi mükellefi). PII.</summary>
    [Sensitive]
    public string? Vkn { get; set; }

    /// <summary>Doğum tarihi (yalnız birey).</summary>
    public DateOnly? BirthDate { get; set; }

    /// <summary>Telefon (E.164 önerilir). PII.</summary>
    [Sensitive]
    public string? Phone { get; set; }

    /// <summary>E-posta.</summary>
    public string? Email { get; set; }

    /// <summary>Tebligat/iletişim adresi (düz metin).</summary>
    public string? AddressLine { get; set; }

    /// <summary>Serbest notlar.</summary>
    public string? Notes { get; set; }

    /// <summary>Etiketler (JSON dizi, örn. ["VIP","Avukat"]).</summary>
    public string? TagsJson { get; set; }

    /// <summary>KVKK açık rıza alındı mı.</summary>
    public bool KvkkConsentGiven { get; set; }

    /// <summary>KVKK rıza zamanı.</summary>
    public DateTimeOffset? KvkkConsentAt { get; set; }

    /// <summary>KVKK rıza kanalı ("Sözleşme" / "Form" / "Sözlü").</summary>
    public string? KvkkConsentChannel { get; set; }

    /// <summary>
    /// Opsiyonel portal eşleşmesi — bu cari kişinin login kullanıcısı (varsa).
    /// F0'da yalnız kolon olarak tutulur, kullanılmaz.
    /// </summary>
    public Guid? LinkedUserId { get; set; }
}
