using CleanTenant.Domain.Tenant.Budgeting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Budgeting;

/// <summary>
/// <c>participation_groups</c> tablosu — katılım grupları (Havuz Kullanıcıları,
/// Ticari Birimler vb.). (CompanyId, Code) benzersiz.
/// </summary>
internal sealed class ParticipationGroupConfiguration : IEntityTypeConfiguration<ParticipationGroup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ParticipationGroup> builder)
    {
        builder.ToTable("participation_groups");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.Code })
            .HasDatabaseName("ix_participation_groups_company_code")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasMany(x => x.Memberships)
            .WithOne()
            .HasForeignKey(m => m.ParticipationGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin concurrency token
        builder.UseXminAsConcurrencyToken();
    }
}
