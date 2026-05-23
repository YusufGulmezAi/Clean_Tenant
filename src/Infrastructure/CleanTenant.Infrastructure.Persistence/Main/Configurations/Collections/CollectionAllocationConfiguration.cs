using CleanTenant.Domain.Tenant.Accruals;
using CleanTenant.Domain.Tenant.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Collections;

/// <summary>
/// <c>collection_allocations</c> tablosu — tahsilatın tahakkuk detaylarına dağıtımı.
/// (CollectionId, AccrualDetailId) benzersiz; AllocatedAmount > 0 CHECK.
/// </summary>
internal sealed class CollectionAllocationConfiguration : IEntityTypeConfiguration<CollectionAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CollectionAllocation> builder)
    {
        builder.ToTable("collection_allocations", t =>
        {
            t.HasCheckConstraint("ck_collection_alloc_amount", "allocated_amount > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CollectionId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.AccrualDetailId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.AccrualDetailId);

        // Bir tahsilatta bir tahakkuk detayı tek satır
        builder.HasIndex(x => new { x.CollectionId, x.AccrualDetailId })
            .HasDatabaseName("ix_collection_alloc_collection_detail")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.AllocatedAmount).HasPrecision(18, 4).IsRequired();

        // AccrualDetail FK — restrict (ödenmiş tahakkuk detayı silinemez)
        builder.HasOne<AccrualDetail>()
            .WithMany()
            .HasForeignKey(x => x.AccrualDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
    }
}
