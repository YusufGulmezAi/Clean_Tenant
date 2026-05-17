namespace CleanTenant.Infrastructure.Caching;

/// <summary>
/// <para>
/// Redis bağlantı ayarları. <c>ConnectionStrings:Redis</c>'ten okunur;
/// <c>RedisSettings:KeyPrefix</c> uygulama isim alanı için.
/// </para>
/// </summary>
public sealed class RedisSettings
{
    /// <summary>Konfigürasyon section adı.</summary>
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis bağlantı dizgesi.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
