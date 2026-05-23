using CleanTenant.Domain.Tenant.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations.Collections;

/// <summary>
/// <c>collections</c> tablosu — tahsilat başlığı. Amount ≥ 0, UnallocatedAmount ≥ 0
/// CHECK; UrlCode unique; xmin concurrency.
/// </summary>
internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("collections", t =>
        {
            t.HasCheckConstraint("ck_collections_amount", "amount >= 0");
            t.HasCheckConstraint("ck_collections_unallocated", "unallocated_amount >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.CompanyId);

        builder.Property(x => x.UnitId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.UnitId);

        builder.Property(x => x.AccountingPeriodId).HasColumnType("uuid").IsRequired();

        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.HasIndex(x => x.UrlCode).IsUnique();

        builder.Property(x => x.PaymentDate).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.Method).HasConversion<short>().IsRequired();
        builder.Property(x => x.CashAccountCodeId).HasColumnType("uuid").IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.UnallocatedAmount).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.JournalEntryId).HasColumnType("uuid");
        builder.Property(x => x.RecordedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.RecordedBy).HasColumnType("uuid");

        builder.HasMany(x => x.Allocations)
            .WithOne()
            .HasForeignKey(a => a.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Audit + soft delete
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // IAggregateRoot → xmin
        builder.UseXminAsConcurrencyToken();
    }
}
