using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Log;

/// <summary>
/// <para>
/// Log DB için EF Core context'i. Yalnız <b>şema oluşturma</b> (migration)
/// amacıyla var; runtime'da Serilog PostgreSQL sink doğrudan
/// <c>NpgsqlConnection</c> üzerinden yazar.
/// </para>
/// </summary>
public sealed class LogDbContext : DbContext
{
    /// <summary>EF design-time için ctor.</summary>
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    /// <summary><c>logs</c> tablosu (Serilog yazar).</summary>
    internal DbSet<LogEntry> Logs => Set<LogEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var log = modelBuilder.Entity<LogEntry>();
        log.ToTable("logs");
        log.HasKey(l => l.Id);
        log.Property(l => l.Id).UseIdentityAlwaysColumn();
        log.Property(l => l.Timestamp).HasColumnType("timestamptz");
        log.Property(l => l.Level);
        log.Property(l => l.Message).HasColumnType("text");
        log.Property(l => l.MessageTemplate).HasColumnType("text");
        log.Property(l => l.Exception).HasColumnType("text");
        log.Property(l => l.Properties).HasColumnType("jsonb");
        log.Property(l => l.TraceId).HasMaxLength(64);
        log.Property(l => l.CorrelationId).HasMaxLength(128);

        log.HasIndex(l => l.Timestamp).IsDescending();
        log.HasIndex(l => l.Level);
        log.HasIndex(l => l.TraceId);
    }
}
