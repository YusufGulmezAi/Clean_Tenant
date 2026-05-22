using CleanTenant.Domain.Tenant.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Main.Configurations;

/// <summary>
/// <c>accounting_settings</c> tablosu için EF Core map'i.
/// Şirket başına tek ayar satırı (CompanyId benzersiz unique index).
/// IAggregateRoot değil — xmin token eklenmez.
/// </summary>
internal sealed class AccountingSettingsConfiguration : IEntityTypeConfiguration<AccountingSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccountingSettings> builder)
    {
        builder.ToTable("accounting_settings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.TenantId).HasColumnType("uuid").IsRequired();
        builder.HasIndex(x => x.TenantId);

        builder.Property(x => x.CompanyId).HasColumnType("uuid").IsRequired();

        // Her şirket için yalnızca bir ayar satırı (soft-delete hariç)
        builder.HasIndex(x => x.CompanyId)
            .HasDatabaseName("ix_accounting_settings_company")
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.Property(x => x.IsActivated).IsRequired();
        builder.Property(x => x.RequireApproval).HasDefaultValue(false);
        builder.Property(x => x.DefaultCurrency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.VatPeriod).HasConversion<short>().IsRequired();
        builder.Property(x => x.EDefterEnabled).HasDefaultValue(false);

        // Audit + soft delete (BaseEntity'den)
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        // NOT UseXminAsConcurrencyToken — IAggregateRoot değil
    }
}
