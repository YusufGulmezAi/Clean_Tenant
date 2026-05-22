namespace CleanTenant.Domain.Tenant.Budgeting.Enums;

/// <summary>
/// <para>
/// Bütçe tipi — site yönetiminin tahakkuk muhasebesinde ayrı yan defter
/// ve hesap kodu altında izlenecek bütçe sınıfları. v0.2.14 — Karar
/// 2026-05-22.
/// </para>
/// <para>
/// Sistem operatörü <c>BudgetTypeMetadata</c> kataloğundan (Catalog DB)
/// her tipin base hesap kodlarını tanımlar (örn. Aidat → 120.01 / 600.01).
/// İlk tahakkuk anında bu base kodun altına otomatik alt hesap üretilir
/// (120.01.001, 120.01.002, …).
/// </para>
/// <para>
/// Yeni bütçe tipi eklemek için: (1) bu enum'a değer ekle, (2) Catalog
/// seed'inde <c>BudgetTypeMetadata</c> satırı ekle, (3) migration ile dağıt.
/// </para>
/// </summary>
public enum BudgetType
{
    /// <summary>Aidat Bütçesi — düzenli aylık aidat (MonthlyEqual veya Installment).</summary>
    Aidat = 0,

    /// <summary>Yatırım Bütçesi — büyük harcama (çatı, asansör vb.) — Installment, m²/Arsa dağılımı.</summary>
    Yatirim = 1,

    /// <summary>Kömür/Yakıt Bütçesi — ısınma giderleri (Ekim-Mart genelde) — Installment.</summary>
    Komur = 2,

    /// <summary>Kuruluş Bütçesi — sitenin açılışındaki tek seferlik kurulum giderleri — Installment.</summary>
    Kurulus = 3
}
