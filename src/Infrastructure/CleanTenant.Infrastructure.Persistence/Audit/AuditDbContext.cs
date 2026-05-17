using CleanTenant.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Infrastructure.Persistence.Audit;

/// <summary>
/// <para>
/// Audit DB için EF Core context'i. Sadece şema (migration) ve seyrek read
/// senaryoları için kullanılır — yazım <c>FullAuditInterceptor</c> üzerinden
/// <b>Dapper raw INSERT</b> ile yapılır (yüksek hacim için EF Core overhead'i
/// kabul edilemez).
/// </para>
/// </summary>
public sealed class AuditDbContext : DbContext
{
    /// <summary>EF design-time için ctor.</summary>
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    /// <summary><c>audit_entries</c> tablosu.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
    }
}
