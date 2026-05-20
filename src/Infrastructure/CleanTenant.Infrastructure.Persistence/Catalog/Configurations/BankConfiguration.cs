using CleanTenant.Domain.LookUp.Banks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

internal sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
{
    public void Configure(EntityTypeBuilder<Bank> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.Property(x => x.FullName).HasColumnType("citext").HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShortName).HasColumnType("citext").HasMaxLength(30).IsRequired();
        builder.Property(x => x.EftCode).HasMaxLength(10).IsRequired(false);
        builder.Property(x => x.HasVirtualPosIntegration).HasDefaultValue(false);
        builder.Property(x => x.HasCorporateCollectionIntegration).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.UrlCode).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.FullName).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
