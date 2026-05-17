namespace CleanTenant.Infrastructure.Persistence.Identifiers;

/// <summary>
/// <para>
/// Bir veri tabanı içindeki tüm üretilmiş URL kodlarının merkezi havuzu.
/// <see cref="Code"/> primary key olarak unique constraint sağlar; çarpışma
/// son güvence olarak burada yakalanır ve <c>UrlCodeGeneratingInterceptor</c>
/// (v0.1.4.b) retry uygular.
/// </para>
/// <para>
/// <b>Konum:</b> Infrastructure.Persistence'ta yaşar; Domain'in iş alanı
/// değil teknik bir altyapı kavramı. Domain entity'leri yalnız <c>UrlCode</c>
/// alanlarını taşır, registry'i bilmez.
/// </para>
/// <para>
/// <b>Ek fayda:</b> <c>OwnerType</c> ve <c>OwnerId</c> ile entity-tipinden
/// bağımsız "code'a göre lookup" mümkün — örn. ileride <c>/lookup?code=xyz</c>
/// gibi bir endpoint yazılabilir.
/// </para>
/// </summary>
public sealed class UrlCodeRegistry
{
    /// <summary>9 karakterlik Base58 URL kodu; primary key.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Bu kodu kullanan entity'nin tip adı (örn. <c>"Tenant"</c>, <c>"User"</c>,
    /// <c>"SupportSession"</c>). String olarak saklanır; reflection ile değil,
    /// interceptor'da entity tipi adından elde edilir.
    /// </summary>
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>Kodu kullanan entity'nin <c>Id</c> değeri.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Kayıt oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
