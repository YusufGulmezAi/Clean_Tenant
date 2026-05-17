using CleanTenant.Domain.Tenant.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>companies</c> tablosu için EF Core map'i. Indeksler + CHECK constraint
/// (VKN formatı) + citext kolon (Name) + xmin RowVersion.
/// </summary>
internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", t =>
        {
            t.HasCheckConstraint("ck_company_vkn_format",
                "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("uuid");

        builder.Property(c => c.UrlCode)
            .HasMaxLength(9)
            .IsRequired();
        builder.HasIndex(c => c.UrlCode).IsUnique();

        builder.Property(c => c.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(c => c.TenantId);

        builder.Property(c => c.Name)
            .HasColumnType("citext")
            .HasMaxLength(256)
            .IsRequired();
        // Tenant içinde Name unique (soft-deleted hariç filter ile)
        builder.HasIndex(c => new { c.TenantId, c.Name })
            .HasDatabaseName("ix_companies_tenant_name")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(c => c.LegalName).HasMaxLength(256);
        builder.Property(c => c.Vkn).HasMaxLength(10).IsFixedLength();
        builder.Property(c => c.Email).HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(32);

        builder.Property(c => c.Status).HasConversion<short>().IsRequired();

        // Audit + soft delete (BaseEntity'den)
        builder.Property(c => c.CreatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(c => c.DeletedAt).HasColumnType("timestamptz");
        builder.Property(c => c.IsDeleted).HasDefaultValue(false);

        // PostgreSQL xmin → uint RowVersion
        builder.UseXminAsConcurrencyToken();
    }
}
