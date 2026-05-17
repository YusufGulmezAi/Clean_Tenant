namespace CleanTenant.Infrastructure.Persistence.Log;

/// <summary>
/// <para>
/// Log DB'deki <c>logs</c> tablosunun infrastructure-only kaydı. Domain entity
/// değildir (BaseEntity/audit/soft-delete yok); yalnız Serilog PostgreSQL sink'in
/// yazacağı şema için EF Core'a tablo tanımı gerek.
/// </para>
/// <para>
/// Yazımı Serilog yapar — uygulama kodu doğrudan bu entity'yi <c>Add</c> etmez.
/// </para>
/// </summary>
internal sealed class LogEntry
{
    /// <summary>BIGSERIAL primary key (Serilog default).</summary>
    public long Id { get; set; }

    /// <summary>UTC zaman damgası.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>LogEventLevel olarak smallint (Verbose=0..Fatal=5).</summary>
    public short Level { get; set; }

    /// <summary>Rendered mesaj.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Mesaj template (örn. <c>"User {UserId} logged in"</c>).</summary>
    public string? MessageTemplate { get; set; }

    /// <summary>Exception ToString() çıktısı; varsa.</summary>
    public string? Exception { get; set; }

    /// <summary>Tüm enricher property'leri jsonb olarak.</summary>
    public string? Properties { get; set; }

    /// <summary>W3C TraceContext trace-id.</summary>
    public string? TraceId { get; set; }

    /// <summary>Application-level correlation id.</summary>
    public string? CorrelationId { get; set; }
}
