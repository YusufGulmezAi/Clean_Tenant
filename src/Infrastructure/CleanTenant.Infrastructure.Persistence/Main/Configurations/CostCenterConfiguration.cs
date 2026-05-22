using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>cost_centers</c> tablosu için EF Core map'i.
/// Şirket içinde benzersiz kod kısıtı, citext arama alanları ve xmin concurrency token.
/// </summary>
internal sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("cost_centers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        // CompanyId + Code şirket içinde benzersiz (soft-delete hariç)
        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .HasDatabaseName("ix_cost_centers_company_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
