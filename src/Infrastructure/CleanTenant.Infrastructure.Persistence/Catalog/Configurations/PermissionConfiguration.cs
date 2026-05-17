using CleanTenant.Domain.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="Permission"/> entity'sinin EF Core eşlemesi.
/// Code unique; Module index'li (modül başlığı altında listeleme için).
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(128);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(p => p.Module)
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(p => p.Module);

        builder.UseXminAsConcurrencyToken();

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
