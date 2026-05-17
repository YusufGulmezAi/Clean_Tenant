using CleanTenant.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="User"/> entity'sinin EF Core eşlemesi.
/// IdentityDbContext'in standart User konfigürasyonunu ek alanlarımızla
/// (UrlCode, FirstName, LastName, son giriş, audit, soft delete, xmin) genişletir.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Identity'nin kendi PK ve index'leri base konfigürasyon tarafından kurulur.

        builder.Property(u => u.UrlCode)
            .IsRequired()
            .HasMaxLength(9)
            .IsFixedLength();
        builder.HasIndex(u => u.UrlCode).IsUnique();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(128);

        // citext: e-posta ve kullanıcı adı case-insensitive sorgulanır
        builder.Property(u => u.Email).HasColumnType("citext");
        builder.Property(u => u.NormalizedEmail).HasColumnType("citext");
        builder.Property(u => u.UserName).HasColumnType("citext");
        builder.Property(u => u.NormalizedUserName).HasColumnType("citext");

        // TCKN/YKN — 11 haneli, sadece rakam; null kabul.
        // YKN ilk hanesi 9'la başlar; TCKN aynı algoritmayı kullanır.
        builder.Property(u => u.Tckn)
            .HasMaxLength(11)
            .IsFixedLength();
        builder.HasIndex(u => u.Tckn).IsUnique().HasFilter("tckn IS NOT NULL");
        builder.Property(u => u.TcknVerified).IsRequired();

        // VKN — 10 haneli, sadece rakam, ilk hane 0 değil; null kabul.
        builder.Property(u => u.Vkn)
            .HasMaxLength(10)
            .IsFixedLength();
        builder.HasIndex(u => u.Vkn).IsUnique().HasFilter("vkn IS NOT NULL");
        builder.Property(u => u.VknVerified).IsRequired();

        // DB seviyesinde format zorunlulukları
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_user_tckn_format", "tckn IS NULL OR tckn ~ '^[0-9]{11}$'");
            t.HasCheckConstraint("ck_user_vkn_format", "vkn IS NULL OR vkn ~ '^[1-9][0-9]{9}$'");
        });

        builder.Property(u => u.LastLoginIp).HasMaxLength(45); // IPv6 max
        builder.Property(u => u.LastLoginAt);

        // xmin
        builder.UseXminAsConcurrencyToken();

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
