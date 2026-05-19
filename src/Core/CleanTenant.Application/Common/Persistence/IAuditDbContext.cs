using CleanTenant.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Persistence;

/// <summary>
/// Audit veri tabanı için Application katmanının read-only soyutlaması.
/// Yazım <c>FullAuditInterceptor</c> üzerinden Dapper raw INSERT ile yapılır;
/// bu arabirim yalnız sorgulama (Explorer, raporlama) içindir.
/// </summary>
public interface IAuditDbContext
{
    /// <summary><c>audit_entries</c> tablosu.</summary>
    DbSet<AuditEntry> AuditEntries { get; }
}
