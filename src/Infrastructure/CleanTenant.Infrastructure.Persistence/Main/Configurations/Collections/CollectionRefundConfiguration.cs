using CleanTenant.Domain.Tenant.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Collections;

/// <summary>
/// <c>collection_refunds</c> tablosu — avans iadesi. Amount &gt; 0 CHECK; xmin concurrency.
/// </summary>
internal sealed class CollectionRefundConfiguration : IEntityTypeConfiguration<CollectionRefund>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CollectionRefund> builder)
    {
        builder.ToTable("collection_refunds", t =>
        {
            t.HasCheckConstraint("ck_collection_refunds_amount", "amount > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.RefundDate).IsRequired();
        builder.Property(x => x.CashAccountCodeId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.AdvanceAccountCodeId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Method).HasConversion<short>().IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.JournalEntryId).HasColumnType("uuid");
        builder.Property(x => x.RefundedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RefundedBy).HasColumnType("uuid");

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin
        builder.UseXminAsConcurrencyToken();
    }
}
