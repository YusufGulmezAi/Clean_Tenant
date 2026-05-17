using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence;

/// <summary>
/// EF Core <c>EntityTypeBuilder</c> için CleanTenant'a özel extension method'lar.
/// Tekrar eden konfigürasyon kalıplarını tek satırda kullanmaya olanak tanır.
/// </summary>
internal static class EfCoreExtensions
{
    /// <summary>
    /// <para>
    /// Entity'nin <c>RowVersion</c> (uint) property'sini PostgreSQL'in <c>xmin</c>
    /// sistem sütununa eşler ve concurrency token olarak işaretler.
    /// </para>
    /// <para>
    /// Optimistic concurrency için: her UPDATE'te PostgreSQL <c>xmin</c>'i
    /// otomatik artırır; EF Core SaveChanges sırasında orijinal değer ile
    /// karşılaştırarak satırın başka bir transaction tarafından değişip
    /// değişmediğini tespit eder. Değişmişse <c>DbUpdateConcurrencyException</c> atar.
    /// </para>
    /// <para>
    /// "RowVersion" adlı CLR property mevcutsa onu kullanır; yoksa shadow
    /// property oluşturur (bizim entity'lerimizde BaseEntity'den miras alarak
    /// her zaman gerçek property mevcut).
    /// </para>
    /// </summary>
    public static EntityTypeBuilder<T> UseXminAsConcurrencyToken<T>(
        this EntityTypeBuilder<T> builder)
        where T : class
    {
        builder.Property<uint>("RowVersion")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        return builder;
    }
}
