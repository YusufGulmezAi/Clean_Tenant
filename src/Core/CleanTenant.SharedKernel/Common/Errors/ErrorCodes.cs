namespace CleanTenant.SharedKernel.Common.Errors;

/// <summary>
/// <para>
/// Genel (modülden bağımsız) hata kodlarının kataloğudur. Her modül kendi
/// hata kodlarını kendi alanında (örn. <c>CleanTenant.Application.Users</c>)
/// tanımlar; bu sınıf yalnızca tüm projede paylaşılan ortak kodları taşır.
/// </para>
/// <para>
/// Format: <c>&lt;MODÜL&gt;-&lt;NNN&gt;</c> — 3 harfli modül kısaltması +
/// 3 haneli sıra numarası.
/// </para>
/// </summary>
public static class ErrorCodes
{
    /// <summary>Genel sistem hataları.</summary>
    public static class General
    {
        /// <summary>Beklenmedik sunucu hatası.</summary>
        public const string Unexpected = "GEN-001";

        /// <summary>Henüz uygulanmamış işlevsellik.</summary>
        public const string NotImplemented = "GEN-002";

        /// <summary>Optimistic concurrency çakışması.</summary>
        public const string ConcurrencyConflict = "GEN-003";

        /// <summary>İşlem zaman aşımına uğradı.</summary>
        public const string Timeout = "GEN-004";
    }

    /// <summary>Doğrulama (validation) hataları.</summary>
    public static class Validation
    {
        /// <summary>Zorunlu alan boş.</summary>
        public const string Required = "VAL-001";

        /// <summary>Format hatalı (e-posta, URL, vb.).</summary>
        public const string InvalidFormat = "VAL-002";

        /// <summary>Değer kabul edilen aralığın dışında.</summary>
        public const string OutOfRange = "VAL-003";

        /// <summary>Maksimum uzunluk aşıldı.</summary>
        public const string MaxLengthExceeded = "VAL-004";
    }
}
