namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// URL'de doğrudan adreslenebilir entity'lerin opt-in işaretleyici arabirimi.
/// Yalnız son kullanıcıya açık bir URL'i olan kaynaklar bu arabirimi
/// implement eder (Tenant, Company, Building, Unit, User, Invoice gibi).
/// </para>
/// <para>
/// Detail / iç entity'ler (InvoiceLineItem, UserRoleAssignment, AuditEntry,
/// LogEntry, RefreshToken, OutboxMessage, vb.) URL ile çağrılmadıkları için
/// bu arabirimi implement <b>etmez</b>. Bu sayede gereksiz alan + index
/// maliyetinden kaçınılır (özellikle yüksek hacimli tablolarda).
/// </para>
/// <para>
/// <c>SaveChangesInterceptor</c> (Faz v0.1.7'de) bu arabirimi implement eden
/// ve <see cref="UrlCode"/>'u boş olan Added entity'lerin koduna
/// <see cref="Identifiers.IUrlCodeGenerator"/> ile değer üretir.
/// </para>
/// </summary>
public interface IHasUrlCode
{
    /// <summary>
    /// 9 karakterlik Base58 (görsel olarak temiz alfabe) URL kodu.
    /// Veri tabanı içinde tekildir (unique constraint + index).
    /// </summary>
    string UrlCode { get; }
}
