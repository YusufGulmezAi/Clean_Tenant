namespace CleanTenant.Domain.Tenant.Budgeting.Enums;

/// <summary>
/// <para>
/// Yıllık bütçenin yaşam döngüsü durumu.
/// </para>
/// <para>
/// MVP'de basit "Yayınla" akışı (Karar #6 — 2026-05-22): Taslak → Yayınlandı.
/// Onay zinciri (Onayda) Wave 3+'a bırakıldı; <c>IApprovalService</c> arabirimi
/// hazırdır, <c>AutoApproveApprovalService</c> ile otomatik onaylanır.
/// </para>
/// <para>
/// İptal yalnızca taslak veya yayınlanmamış sürümler için anlamlıdır. Yayınlı
/// versiyonlar geriye dönülemez şekilde immutable kalır; düzeltme için
/// revizyon (yeni <c>BudgetVersion</c>) üretilir.
/// </para>
/// </summary>
public enum BudgetStatus
{
    /// <summary>Taslak — kalemler eklenebilir, düzenlenebilir.</summary>
    Draft = 0,

    /// <summary>Yayınlandı — en az bir <c>BudgetVersion</c> aktif; tahakkuk üretilebilir.</summary>
    Published = 1,

    /// <summary>İptal — bütçe kullanılmayacak. Yayınlı versiyonlar varsa geçmiş kayıtlar dokunulmaz.</summary>
    Cancelled = 2
}
