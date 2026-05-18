using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.LookUp.ResidentialTypes;

/// <summary>Mesken tipi (residential type) — Daire, Ofis, Dükkan vb. Sistem geneli sabit kütüphanesi.</summary>
public sealed class ResidentialType : BaseEntity, IAggregateRoot, IHasUrlCode
{
    /// <summary>9 karakterlik Base58 URL kodu.</summary>
    public string UrlCode { get; set; } = string.Empty;

    /// <summary>Mesken tipi adı (örn. Daire, Ofis, Dükkan). Max 15 karakter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Opsiyonel açıklama. Max 250 karakter.</summary>
    public string? Description { get; set; }
}
