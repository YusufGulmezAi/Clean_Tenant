using CleanTenant.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanTenant.Infrastructure.Persistence.Audit.Configurations;

/// <summary>
/// <c>audit_entries</c> tablosu için EF Core map'i. Indeksler:
/// <list type="bullet">
///   <item><c>(tenant_id, timestamp DESC)</c> — tenant-bazlı tarih sıralı sorgu.</item>
///   <item><c>(entity_type, entity_id)</c> — bir entity'nin geçmişi.</item>
///   <item><c>support_session_id</c> — Support Mode oturumlarına bağlı işlemler.</item>
///   <item>jsonb <c>changes</c> için GIN — delta sorguları.</item>
/// </list>
/// </summary>
internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnType("uuid");

        builder.Property(e => e.Timestamp).HasColumnType("timestamptz");
        builder.Property(e => e.UserId).HasColumnType("uuid");
        builder.Property(e => e.UserEmail).HasMaxLength(256);
        builder.Property(e => e.UserFullName).HasMaxLength(256);
        builder.Property(e => e.TenantId).HasColumnType("uuid");
        builder.Property(e => e.TenantName).HasMaxLength(256);
        builder.Property(e => e.ScopeLevel).HasMaxLength(32);
        builder.Property(e => e.CompanyId).HasColumnType("uuid");
        builder.Property(e => e.UnitId).HasColumnType("uuid");
        builder.Property(e => e.PersonaSide).HasMaxLength(32);
        builder.Property(e => e.RolesJson).HasColumnType("jsonb");

        builder.Property(e => e.SupportSessionId).HasColumnType("uuid");
        builder.Property(e => e.ImpersonatedByUserId).HasColumnType("uuid");

        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.BrowserName).HasMaxLength(64);
        builder.Property(e => e.BrowserVersion).HasMaxLength(32);
        builder.Property(e => e.OperatingSystem).HasMaxLength(64);
        builder.Property(e => e.DeviceType).HasMaxLength(32);
        builder.Property(e => e.AcceptLanguage).HasMaxLength(128);
        builder.Property(e => e.Referer).HasMaxLength(512);
        builder.Property(e => e.Country).HasMaxLength(8);
        builder.Property(e => e.City).HasMaxLength(128);

        builder.Property(e => e.TraceId).HasMaxLength(64);
        builder.Property(e => e.CorrelationId).HasMaxLength(128);
        builder.Property(e => e.RequestPath).HasMaxLength(512);
        builder.Property(e => e.RequestMethod).HasMaxLength(16);

        builder.Property(e => e.EnvironmentName).HasMaxLength(32);
        builder.Property(e => e.MachineName).HasMaxLength(128);
        builder.Property(e => e.ApplicationName).HasMaxLength(128);
        builder.Property(e => e.ApplicationVersion).HasMaxLength(32);

        builder.Property(e => e.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(e => e.EntityId).HasColumnType("uuid");
        builder.Property(e => e.Action).HasConversion<short>();
        builder.Property(e => e.ChangesJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Timestamp })
            .HasDatabaseName("ix_audit_entries_tenant_timestamp")
            .IsDescending(false, true);

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("ix_audit_entries_entity");

        builder.HasIndex(e => e.SupportSessionId)
            .HasDatabaseName("ix_audit_entries_support_session")
            .HasFilter("support_session_id IS NOT NULL");
    }
}
