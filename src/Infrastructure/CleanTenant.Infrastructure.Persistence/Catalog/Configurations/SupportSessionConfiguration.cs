using CleanTenant.Domain.Identity.Support;
using CleanTenant.Domain.Identity.Tenants;
using CleanTenant.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Catalog.Configurations;

/// <summary>
/// <see cref="SupportSession"/> entity'sinin EF Core eşlemesi.
/// UrlCode unique; Reason min 20 karakter CHECK constraint; operatör ve
/// hedef tenant üzerinden performans index'leri.
/// </summary>
public sealed class SupportSessionConfiguration : IEntityTypeConfiguration<SupportSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SupportSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.UrlCode)
            .IsRequired()
            .HasMaxLength(9)
            .IsFixedLength();
        builder.HasIndex(s => s.UrlCode).IsUnique();

        builder.Property(s => s.Mode)
            .IsRequired()
            .HasConversion<short>();

        builder.Property(s => s.Reason)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(s => s.StartedAt).IsRequired();
        builder.Property(s => s.WriteActionCount).IsRequired();
        builder.Property(s => s.CustomerNotified).IsRequired();

        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasMaxLength(512);

        // Performans index'leri
        builder.HasIndex(s => new { s.OperatorUserId, s.StartedAt })
            .HasDatabaseName("ix_support_session_operator_started");
        builder.HasIndex(s => new { s.TargetTenantId, s.StartedAt })
            .HasDatabaseName("ix_support_session_tenant_started");

        // Foreign key'ler
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.OperatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TargetTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reason min 20 karakter
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_support_session_reason_minlength",
            "char_length(reason) >= 20"));

        builder.UseXminAsConcurrencyToken();

        // SupportSession kayıtları kalıcı denetim kayıtlarıdır; soft delete uygulanmaz.
    }
}
