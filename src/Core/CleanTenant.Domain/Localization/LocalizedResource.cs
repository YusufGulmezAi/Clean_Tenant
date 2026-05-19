using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.Localization;

/// <summary>
/// <para>
/// CleanTenant'ın çok dilli string kaynak entity'si. Her satır <see cref="Key"/>
/// (kararlı tanımlayıcı, örn. <c>"User.Read.Description"</c>) ile bir
/// <see cref="Culture"/> (BCP-47, örn. <c>"tr-TR"</c>) için <see cref="Value"/>
/// metnini taşır. Composite unique: <c>(Key, Culture)</c>.
/// </para>
/// <para>
/// <b>Fallback zinciri (v0.2.10):</b> İstenen kültürde yoksa <c>en-US</c>'a düş,
/// o da yoksa <c>tr-TR</c>'ye düş, o da yoksa key'i raw göster (dev için).
/// </para>
/// <para>
/// <b>Machine translation iş akışı:</b> Yeni key eklenince TR varsayılan değer
/// (geliştirici/seeder tarafından) yazılır; diğer kültürler için makine çevirisi
/// stub'ı oluşturulur ve <see cref="IsMachineTranslated"/>=true işaretlenir.
/// Sistem yöneticisi <c>/system/localization</c> sayfasında elle revize edip
/// flag'i kaldırır.
/// </para>
/// <para>
/// <see cref="BaseEntity"/>'den audit (CreatedAt/By, UpdatedAt/By), soft delete
/// (IsDeleted, DeletedAt/By) ve RowVersion otomatik gelir.
/// </para>
/// </summary>
public sealed class LocalizedResource : BaseEntity
{
    /// <summary>
    /// Çeviri anahtarı (dot-notation, kararlı tanımlayıcı).
    /// Örn. <c>"User.Read.Description"</c>, <c>"Roles.New.SubmitButton"</c>.
    /// Max 256 karakter.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// BCP-47 culture kodu, örn. <c>"tr-TR"</c>, <c>"en-US"</c>, <c>"ar-SA"</c>,
    /// <c>"ru-RU"</c>, <c>"de-DE"</c>. Max 16 karakter (yeterli marj).
    /// </summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Bu kültür için çeviri değeri. Plural form'lar veya parametreli string'ler
    /// uygulamada interpolation ile çözülür (örn. <c>"Hoşgeldin, {0}."</c>).
    /// Max 4000 karakter (Postgres TEXT'e yakın).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Bu değer makine çevirisi mi (Google/DeepL placeholder)? Sistem yöneticisi
    /// elle revize edip false yapana kadar UI'da "revizyon gerekli" uyarısı
    /// gösterilir.
    /// </summary>
    public bool IsMachineTranslated { get; set; }
}
