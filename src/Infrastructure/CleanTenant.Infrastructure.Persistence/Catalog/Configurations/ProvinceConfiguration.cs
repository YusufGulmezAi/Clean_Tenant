using CleanTenant.Domain.LookUp.Provinces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

internal sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Name).HasColumnType("citext").HasMaxLength(100).IsRequired();
        builder.Property(x => x.PlateCode).IsRequired(false);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.UrlCode).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.Name).HasFilter("\"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasMany(x => x.Districts)
            .WithOne(x => x.Province)
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
