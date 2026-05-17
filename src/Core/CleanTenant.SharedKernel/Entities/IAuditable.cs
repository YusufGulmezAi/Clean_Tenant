namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// Kim tarafından, ne zaman oluşturulduğu / güncellendiği bilgilerini
/// taşıyan entity'lerin arabirimidir.
/// </para>
/// <para>
/// <c>SaveChangesInterceptor</c> (Faz v0.1.7'de) bu arabirimi implement
/// eden entity'lerin alanlarını otomatik olarak <see cref="Time.IClock"/>
/// ve <see cref="Context.IUserContext"/>'ten okuyup doldurur. Handler'lar
/// bu alanları manuel olarak setlemez.
/// </para>
/// </summary>
public interface IAuditable
{
    /// <summary>Kayıt oluşturulma anı (UTC).</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Kaydı oluşturan kullanıcı kimliği; sistem işlemiyse null.</summary>
    Guid? CreatedBy { get; set; }

    /// <summary>Son güncelleme anı (UTC); hiç güncellenmediyse null.</summary>
    DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Son güncellemeyi yapan kullanıcı kimliği; sistem işlemiyse null.</summary>
    Guid? UpdatedBy { get; set; }
}
