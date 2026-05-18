using CleanTenant.Domain.LookUp.Neighborhoods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

internal sealed class NeighborhoodConfiguration : IEntityTypeConfiguration<Neighborhood>
{
    public void Configure(EntityTypeBuilder<Neighborhood> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UrlCode).HasMaxLength(9).IsRequired();
        builder.Property(x => x.Name).HasColumnType("citext").HasMaxLength(100).IsRequired();
        builder.Property(x => x.DistrictId).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamptz").IsRequired(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.UrlCode).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.DistrictId, x.Name }).HasFilter("\"IsDeleted\" = false");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.District)
            .WithMany(x => x.Neighborhoods)
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
