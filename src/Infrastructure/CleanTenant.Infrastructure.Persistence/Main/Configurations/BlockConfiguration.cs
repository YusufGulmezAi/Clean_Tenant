using CleanTenant.Domain.Tenant.BuildingSchema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

internal sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnType("uuid");

        builder.Property(b => b.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(b => b.UrlCode).IsUnique();

        builder.Property(b => b.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(b => b.TenantId);

        builder.Property(b => b.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(b => b.CompanyId);

        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(b => new { b.CompanyId, b.Name })
            .HasDatabaseName("ix_blocks_company_name")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(b => b.SortOrder).IsRequired();

        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.CreatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(b => b.DeletedAt).HasColumnType("timestamptz");
        builder.Property(b => b.IsDeleted).HasDefaultValue(false);

        builder.UseXminAsConcurrencyToken();
    }
}
