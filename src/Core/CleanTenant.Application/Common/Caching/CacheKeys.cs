namespace CleanTenant.Application.Common.Caching;

/// <summary>
/// <para>
/// Cache key convention'ını tek noktada toplar. Pattern:
/// <c>{KeyPrefix}:{aggregate}:{operation}:{params}</c>.
/// </para>
/// <para>
/// <b>KeyPrefix</b> versioning içerir (örn. <c>cleantenant:v1</c>). Schema kırıcı
/// değişiklik yapılırsa version bump → eski cache otomatik invalidate (orphan
/// kalır, TTL ile temizlenir).
/// </para>
/// <para>
/// <b>Prefix-based invalidation</b>: Üst seviye prefix'le tüm alt key'leri silmek
/// için kullanılır (örn. <c>RemoveByPrefixAsync(CacheKeys.Tenant.Prefix)</c> →
/// tüm tenant cache'i temizler).
/// </para>
/// </summary>
public static class CacheKeys
{
    /// <summary>Cache anahtarlarının ortak ön eki + sürüm. Schema kırıcı değişimde bump'lanır.</summary>
    public const string KeyPrefix = "cleantenant:v1";

    /// <summary>Tenant (Yönetim) ile ilgili cache anahtarları.</summary>
    public static class Tenant
    {
        /// <summary>Tüm Tenant cache key'lerinin ortak ön eki — prefix invalidation için.</summary>
        public const string Prefix = $"{KeyPrefix}:catalog:tenants";

        /// <summary>Tüm aktif tenant'ların projection listesi.</summary>
        public static readonly string AllActive = $"{Prefix}:all-active";

        /// <summary>Id ile tek tenant projection.</summary>
        public static string ById(Guid id) => $"{Prefix}:by-id:{id:N}";

        /// <summary>UrlCode ile tek tenant projection.</summary>
        public static string ByUrlCode(string urlCode) => $"{Prefix}:by-url-code:{urlCode}";

        /// <summary>Detail (Edit formu için tam alan seti) projection.</summary>
        public static string DetailById(Guid id) => $"{Prefix}:detail:{id:N}";
    }

    /// <summary>Company (Site) ile ilgili cache anahtarları (Main DB).</summary>
    public static class Company
    {
        /// <summary>Tüm Company cache key'lerinin ortak ön eki — prefix invalidation için.</summary>
        public const string Prefix = $"{KeyPrefix}:main:companies";

        /// <summary>Tüm site'ler (Sistem operatör cross-tenant görünümü için).</summary>
        public static readonly string AllGlobal = $"{Prefix}:all-global";

        /// <summary>Belirli Yönetim'in altındaki site'ler.</summary>
        public static string ByTenant(Guid tenantId) => $"{Prefix}:by-tenant:{tenantId:N}";

        /// <summary>Id ile tek company projection.</summary>
        public static string ById(Guid id) => $"{Prefix}:by-id:{id:N}";

        /// <summary>UrlCode ile tek company projection.</summary>
        public static string ByUrlCode(string urlCode) => $"{Prefix}:by-url-code:{urlCode}";

        /// <summary>Detail (Edit formu için tam alan seti) projection.</summary>
        public static string DetailById(Guid id) => $"{Prefix}:detail:{id:N}";
    }

    /// <summary>Kullanıcı (user) bazlı projection cache anahtarları.</summary>
    public static class User
    {
        /// <summary>Tüm User cache key'lerinin ortak ön eki — prefix invalidation için.</summary>
        public const string Prefix = $"{KeyPrefix}:catalog:user";

        /// <summary>
        /// Kullanıcının erişebileceği bağlam (Yönetim/Site) listesi.
        /// TenantSwitcher dropdown'unda + persona seçimi'nde kullanılır.
        /// UserRoleAssignments değiştiğinde invalidate edilmeli.
        /// </summary>
        public static string Contexts(Guid userId) => $"{Prefix}:contexts:{userId:N}";
    }

    /// <summary>Pub/sub channel adı — multi-instance L1 invalidation.</summary>
    public const string InvalidationChannel = $"{KeyPrefix}:cache-invalidate";
}
