namespace CleanTenant.Domain.Tenant.Accounting.Enums;

/// <summary>
/// TDHP (Tek Düzen Hesap Planı) ana hesap sınıfı.
/// Her sınıf, hesap kodunun ilk hanesiyle örtüşür.
/// </summary>
public enum AccountClass
{
    /// <summary>1xx — Dönen varlıklar (Current Assets).</summary>
    CurrentAsset = 1,

    /// <summary>2xx — Duran varlıklar (Non-Current Assets).</summary>
    NonCurrentAsset = 2,

    /// <summary>3xx — Kısa vadeli yabancı kaynaklar (Short-Term Liabilities).</summary>
    ShortTermLiability = 3,

    /// <summary>4xx — Uzun vadeli yabancı kaynaklar (Long-Term Liabilities).</summary>
    LongTermLiability = 4,

    /// <summary>5xx — Özkaynak (Equity).</summary>
    Equity = 5,

    /// <summary>6xx — Gelir tablosu hesapları (Income Statement).</summary>
    IncomeStatement = 6,

    /// <summary>7xx — Maliyet yansıtma hesapları (Cost Allocation).</summary>
    CostAllocation = 7,

    /// <summary>9xx — Nazım hesaplar (Off-Balance / Memorandum).</summary>
    OffBalance = 9
}
