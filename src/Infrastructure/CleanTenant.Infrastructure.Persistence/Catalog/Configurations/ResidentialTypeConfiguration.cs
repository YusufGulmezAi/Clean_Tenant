using CleanTenant.Domain.LookUp.ResidentialTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

internal sealed class ResidentialTypeConfiguration : IEntityTypeConfiguration<ResidentialType>
{
    public void Configure(EntityTypeBuilder<ResidentialType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Name).HasColumnType("citext").HasMaxLength(15).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(250).IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.UrlCode).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
