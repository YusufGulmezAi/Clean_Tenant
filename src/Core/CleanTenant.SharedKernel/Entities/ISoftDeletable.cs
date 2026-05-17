namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// Soft-delete (mantıksal silme) destekleyen entity'lerin arabirimidir.
/// Kayıt fiziksel olarak silinmek yerine <see cref="IsDeleted"/> alanı true
/// yapılır.
/// </para>
/// <para>
/// EF Core global query filter'ı bu arabirimi implement eden entity'ler için
/// otomatik olarak <c>e =&gt; !e.IsDeleted</c> uygular; silinen kayıtlar
/// olağan sorgularda görünmez. Soft-delete ihtiyacı olmayan entity'ler
/// (örn. join tablosu, log entry, outbox message) bu arabirimi implement
/// etmez ve gerektiğinde gerçek silmeye tabi tutulur.
/// </para>
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Mantıksal silme bayrağı.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Silinme anı (UTC); silinmediyse null.</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Silen kullanıcı kimliği; sistem işlemiyse null.</summary>
    Guid? DeletedBy { get; set; }
}
