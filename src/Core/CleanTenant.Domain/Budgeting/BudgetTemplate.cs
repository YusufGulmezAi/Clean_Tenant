using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Budgeting;

/// <summary>
/// <para>
/// Tenant'lar-arası paylaşılabilir bütçe şablonu (Catalog DB — app-global, tenant
/// izolasyonu yok; ChartOfAccountsTemplate / BudgetTypeMetadata deseniyle aynı).
/// Bir tenant hazırladığı bütçenin tasarımını şablon olarak yayınlar; başka
/// tenant'lar bu şablondan sitelerinin ilk bütçesini oluşturur.
/// </para>
/// <para>
/// <b>Taşınabilirlik:</b> Şablon FK referansı taşımaz; kalemler
/// <see cref="BudgetTemplateLine"/>'da kod/ad + plan olarak denormalize edilir.
/// <b>Gizlilik:</b> şablonlar yapı-only'dir (tutar taşımaz).
/// </para>
/// </summary>
public sealed class BudgetTemplate : BaseEntity, IHasUrlCode
{
    /// <summary>Şablonu yayınlayan tenant. <c>null</c> = sistem küratörlü resmi şablon.</summary>
    public Guid? OwnerTenantId { get; set; }

    /// <summary>Görünürlük (Private / Public).</summary>
    public TemplateVisibility Visibility { get; set; } = TemplateVisibility.Private;

    /// <summary>Şablonun bütçe tipi (Aidat / Yatırım / ...). Site türüne göre filtrelenir.</summary>
    public BudgetType Type { get; set; }

    /// <summary>9 karakterlik Base58 kısa kod (paylaşım linki).</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Şablon adı.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Açıklama. Opsiyonel.</summary>
    public string? Description { get; set; }

    /// <summary>Kaynak site/tenant etiketi (görsel amaçlı, opsiyonel; denormalize).</summary>
    public string? SourceLabel { get; set; }

    /// <summary>Şablon kalemleri (yapı-only).</summary>
    public ICollection<BudgetTemplateLine> Lines { get; set; } = [];
}
