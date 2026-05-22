using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

internal sealed class LandConfiguration : IEntityTypeConfiguration<Land>
{
    public void Configure(EntityTypeBuilder<Land> builder)
    {
        builder.ToTable("lands");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnType("uuid");

        builder.Property(l => l.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(l => l.UrlCode).IsUnique();

        builder.Property(l => l.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(l => l.TenantId);

        builder.Property(l => l.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(l => l.CompanyId);

        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(l => new { l.CompanyId, l.Name })
            .HasDatabaseName("ix_lands_company_name")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(l => l.SortOrder).IsRequired();

        builder.HasOne(l => l.Company)
            .WithMany(c => c.Lands)
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.CreatedAt).HasColumnType("timestamptz");
        builder.Property(l => l.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(l => l.DeletedAt).HasColumnType("timestamptz");
        builder.Property(l => l.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
