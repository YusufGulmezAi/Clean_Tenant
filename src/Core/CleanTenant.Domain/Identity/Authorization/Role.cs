using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Entities;
using Microsoft.AspNetCore.Identity;

namespace CleanTenant.Domain.Identity.Authorization;

/// <summary>
/// <para>
/// CleanTenant rol entity'si. ASP.NET Core Identity'nin
/// <see cref="IdentityRole{TKey}"/> sınıfından miras alır;
/// <see cref="IdentityRole{TKey}.Name"/> ve <c>NormalizedName</c> standart
/// olarak gelir.
/// </para>
/// <para>
/// <b>Scope hiyerarşisi:</b> <see cref="Scope"/> seviyesi roller'in hangi
/// kademede atanabileceğini belirler (System / Tenant / Company / Unit).
/// Aynı isim farklı scope'larda olabilir (örn. "Admin" rolü Tenant ve
/// Company seviyelerinde ayrı tanımlı); bu nedenle unique index
/// <c>(NormalizedName, Scope)</c> bileşik.
/// </para>
/// <para>
/// <b>Built-in:</b> <see cref="IsBuiltIn"/> true olan roller (System rolleri
/// + temel Tenant/Company/Unit rolleri) seed sırasında oluşturulur ve
/// silinemez / yeniden adlandırılamaz. Permission haritalaması Faz 1'de
/// ManagementApp "Rol Yönetimi" ekranıyla yapılır.
/// </para>
/// </summary>
public sealed class Role : IdentityRole<Guid>, IAuditable, ISoftDeletable, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu (rol detay sayfası için).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Rolün uygulanabileceği yetki kapsamı seviyesi.</summary>
    public ScopeLevel Scope { get; set; }

    /// <summary>Rol açıklaması (UI'da kullanıcıya gösterilir; lokalize edilebilir).</summary>
    public string? Description { get; set; }

    /// <summary>
    /// True ise sistem tarafından seed sırasında oluşturulmuştur;
    /// silinemez / yeniden adlandırılamaz. UI'da düzenleme kapalı.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public Guid? UpdatedBy { get; set; }

    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }

    /// <summary>PostgreSQL xmin sistem sütununa eşlenen concurrency token.</summary>
    public uint RowVersion { get; set; }
}
