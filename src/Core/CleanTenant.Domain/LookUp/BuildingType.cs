using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.BuildingTypes;

/// <summary>Yapı tipi (building type) — Apartman, AVM, Klinik vb. Sistem geneli sabit kütüphanesi.</summary>
public sealed class BuildingType : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Yapı tipi adı (örn. Apartman, AVM, Klinik). Max 100 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opsiyonel açıklama. Max 250 karakter.</summary>
    public string? Description { get; set; }
}
