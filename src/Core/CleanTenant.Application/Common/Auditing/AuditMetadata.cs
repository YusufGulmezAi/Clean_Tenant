namespace CleanTenant.Application.Common.Auditing;

/// <summary>
/// <para>
/// Audit kaydı ya da structured log üretirken üzerine eklenecek tüm bağlam
/// bilgilerinin paketi. <c>FullAuditInterceptor</c> her satırı bu metadata ile
/// zenginleştirir; Serilog enricher'ları aynı veriyi <c>properties</c> jsonb'sine
/// taşır.
/// </para>
/// <para>
/// Anonim/sistem çağrılarında bütün kullanıcı alanları <c>null</c> olur;
/// environment alanları her zaman doludur.
/// </para>
/// </summary>
public sealed record AuditMetadata
{
    // ── Kullanıcı ──
    /// <summary>Aktif kullanıcı (sistem ise null).</summary>
    public Guid? UserId { get; init; }
    /// <summary>Kullanıcı e-postası.</summary>
    public string? UserEmail { get; init; }
    /// <summary>Kullanıcı tam adı (varsa).</summary>
    public string? UserFullName { get; init; }

    /// <summary>Aktif tenant scope'unun tenant kimliği.</summary>
    public Guid? TenantId { get; init; }
    /// <summary>Tenant adı (denormalize için).</summary>
    public string? TenantName { get; init; }
    /// <summary>Aktif scope seviyesi.</summary>
    public string? ScopeLevel { get; init; }
    /// <summary>Aktif scope'taki company kimliği.</summary>
    public Guid? CompanyId { get; init; }
    /// <summary>Aktif scope'taki unit kimliği.</summary>
    public Guid? UnitId { get; init; }
    /// <summary>Login persona.</summary>
    public string? PersonaSide { get; init; }
    /// <summary>Aktif scope'taki rol adları.</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>Aktif Support session ise true.</summary>
    public bool IsSystemSession { get; init; }
    /// <summary>Aktif support session kimliği.</summary>
    public Guid? SupportSessionId { get; init; }
    /// <summary>Impersonation yapıldıysa asıl operatör.</summary>
    public Guid? ImpersonatedByUserId { get; init; }

    // ── Lokasyon ──
    /// <summary>İstemci IP'si.</summary>
    public string? IpAddress { get; init; }
    /// <summary>Ham User-Agent.</summary>
    public string? UserAgent { get; init; }
    /// <summary>Tarayıcı ailesi.</summary>
    public string? BrowserName { get; init; }
    /// <summary>Tarayıcı sürümü.</summary>
    public string? BrowserVersion { get; init; }
    /// <summary>İşletim sistemi ailesi.</summary>
    public string? OperatingSystem { get; init; }
    /// <summary>Cihaz tipi.</summary>
    public string? DeviceType { get; init; }
    /// <summary>HTTP Accept-Language başlığı.</summary>
    public string? AcceptLanguage { get; init; }
    /// <summary>HTTP Referer başlığı.</summary>
    public string? Referer { get; init; }
    /// <summary>GeoIP country (Faz 1+).</summary>
    public string? Country { get; init; }
    /// <summary>GeoIP city (Faz 1+).</summary>
    public string? City { get; init; }

    // ── Request bağlamı ──
    /// <summary>W3C TraceContext trace-id.</summary>
    public string? TraceId { get; init; }
    /// <summary>Correlation id (HTTP header'dan veya internal).</summary>
    public string? CorrelationId { get; init; }
    /// <summary>Request path (örn. /api/v1/auth/login).</summary>
    public string? RequestPath { get; init; }
    /// <summary>HTTP method.</summary>
    public string? RequestMethod { get; init; }

    // ── Environment ──
    /// <summary>Çalışma ortamı adı.</summary>
    public string EnvironmentName { get; init; } = "Unknown";
    /// <summary>Sunucu hostname.</summary>
    public string MachineName { get; init; } = string.Empty;
    /// <summary>Application adı.</summary>
    public string ApplicationName { get; init; } = "CleanTenant";
    /// <summary>Application sürümü (assembly).</summary>
    public string ApplicationVersion { get; init; } = "0.0.0";
    /// <summary>OS process kimliği.</summary>
    public int ProcessId { get; init; }
    /// <summary>İşlemi yapan thread kimliği.</summary>
    public int ThreadId { get; init; }
}
