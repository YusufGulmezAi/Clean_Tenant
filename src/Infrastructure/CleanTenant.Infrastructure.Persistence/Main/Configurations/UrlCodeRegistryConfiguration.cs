using CleanTenant.Infrastructure.Persistence.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <see cref="UrlCodeRegistry"/> entity'sinin Main DB için EF Core eşlemesi.
/// UrlCode üretici interceptor'ın çarpışma kontrolü için bu tablo gereklidir.
/// </summary>
public sealed class UrlCodeRegistryConfiguration : IEntityTypeConfiguration<UrlCodeRegistry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UrlCodeRegistry> builder)
    {
        builder.HasKey(r => r.Code);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(9)
            .IsFixedLength();

        builder.Property(r => r.OwnerType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.OwnerId).IsRequired();

        builder.Property(r => r.CreatedAt).IsRequired();

        // Bir OwnerType + OwnerId çiftinin tek code'u olmalı.
        builder.HasIndex(r => new { r.OwnerType, r.OwnerId }).IsUnique();
    }
}
