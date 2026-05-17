namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// <para>
/// Tüm CleanTenant entity'lerinin ortak temel sınıfıdır. Audit alanlarını,
/// soft-delete bayrağını ve optimistic concurrency token'ını taşır.
/// </para>
/// <para>
/// <b>Taşımadıkları:</b>
/// <list type="bullet">
///   <item><c>UrlCode</c> — opt-in; <see cref="IHasUrlCode"/> implement eden entity'lerde.</item>
///   <item><c>TenantId</c> — opt-in; <see cref="ITenantScoped"/> implement eden entity'lerde.</item>
/// </list>
/// </para>
/// <para>
/// <b>Doluma süreci:</b> <c>Id</c>, <c>CreatedAt/By</c>, <c>UpdatedAt/By</c>
/// alanları <c>SaveChangesInterceptor</c> (Faz v0.1.7) tarafından otomatik
/// doldurulur — handler bu alanları manuel atamaz. <c>RowVersion</c> EF Core
/// tarafından PostgreSQL <c>xmin</c> sistem sütununa eşlenir (Faz v0.1.4).
/// </para>
/// </summary>
public abstract class BaseEntity : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Entity'nin benzersiz kimliği. UUID v7 (zaman-sıralı). Property
    /// initializer ile new'lendiği an üretilir; bu sayede EF Core ChangeTracker
    /// IdentityMap'i birden çok entity'yi aynı <c>Guid.Empty</c> olarak görmez.
    /// EF DB'den materialize ederken setter'ı kullanır, initializer yine
    /// çalışır ama EF set ettiği değer geçerli olur.
    /// </summary>
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    /// <summary>Kayıt oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Kaydı oluşturan kullanıcı kimliği; sistem işlemiyse null.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Son güncelleme anı (UTC); hiç güncellenmediyse null.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Son güncellemeyi yapan kullanıcı kimliği; sistem işlemiyse null.</summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>Soft-delete bayrağı; global query filter'da gizlemek için kullanılır.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Silinme anı (UTC); silinmediyse null.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Silen kullanıcı kimliği; sistem işlemiyse null.</summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// Optimistic concurrency token. PostgreSQL <c>xmin</c> sistem sütununa
    /// EF Core configuration ile eşlenir. <c>uint</c> tipi xmin'in 32-bit
    /// transaction ID alanıyla bire bir uyumlu.
    /// </summary>
    public uint RowVersion { get; set; }
}
