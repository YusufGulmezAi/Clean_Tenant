using CleanTenant.Domain.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="LocalizedResource"/> entity'sinin EF Core eşlemesi.
/// (Key, Culture) bileşik unique; lookup için Key index'li.
/// </summary>
public sealed class LocalizedResourceConfiguration : IEntityTypeConfiguration<LocalizedResource>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LocalizedResource> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Key)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.Culture)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(r => r.Value)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(r => r.IsMachineTranslated)
            .IsRequired();

        // Composite unique: bir key + culture çifti tek değere sahip olabilir
        builder.HasIndex(r => new { r.Key, r.Culture })
            .IsUnique()
            .HasDatabaseName("ix_localized_resource_key_culture");

        // Hızlı "tüm Key için diller" lookup'ı için
        builder.HasIndex(r => r.Culture)
            .HasDatabaseName("ix_localized_resource_culture");

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
