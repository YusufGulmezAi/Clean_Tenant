namespace CleanTenant.Domain.Auditing;

/// <summary>
/// <para>
/// Audit DB'de tek bir DB write operasyonunu tutan kayıt. BaseEntity'den
/// türemez — audit'in audit'i yoktur. Yalnız <see cref="Id"/> + zaman taşır.
/// </para>
/// <para>
/// <b>Denormalize alanlar:</b> User adı, tenant adı, persona, scope, browser,
/// OS bilgileri her satıra kopyalanır. Audit DB analizinde join gerektirmeden
/// sorgu yazılabilsin diye.
/// </para>
/// </summary>
public sealed class AuditEntry
{
    /// <summary>UUID v7 (zaman-sıralı).</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    // ── Zaman ──
    /// <summary>UTC, microsecond hassasiyetli olay anı.</summary>
    public DateTimeOffset Timestamp { get; set; }

    // ── Kullanıcı (denormalize) ──
    /// <summary>İşlemi yapan kullanıcı (sistem ise null).</summary>
    public Guid? UserId { get; set; }
    /// <summary>Kullanıcı e-postası (denormalize).</summary>
    public string? UserEmail { get; set; }
    /// <summary>Kullanıcı tam adı (denormalize).</summary>
    public string? UserFullName { get; set; }

    /// <summary>Aktif scope'taki tenant kimliği.</summary>
    public Guid? TenantId { get; set; }
    /// <summary>Tenant adı (denormalize).</summary>
    public string? TenantName { get; set; }
    /// <summary>Aktif scope seviyesi (System/Tenant/Company/Unit).</summary>
    public string? ScopeLevel { get; set; }
    /// <summary>Aktif scope'taki company kimliği.</summary>
    public Guid? CompanyId { get; set; }
    /// <summary>Aktif scope'taki unit kimliği.</summary>
    public Guid? UnitId { get; set; }
    /// <summary>Login persona (Management/Portal).</summary>
    public string? PersonaSide { get; set; }
    /// <summary>Aktif scope'taki rol listesi (JSON array).</summary>
    public string? RolesJson { get; set; }

    /// <summary>Support Mode session ise true.</summary>
    public bool IsSystemSession { get; set; }
    /// <summary>Aktif Support Session kimliği (varsa).</summary>
    public Guid? SupportSessionId { get; set; }
    /// <summary>Impersonation aktifse asıl operatör kimliği.</summary>
    public Guid? ImpersonatedByUserId { get; set; }

    // ── Lokasyon (client) ──
    /// <summary>İstemci IP'si.</summary>
    public string? IpAddress { get; set; }
    /// <summary>Ham User-Agent string'i.</summary>
    public string? UserAgent { get; set; }
    /// <summary>UAParser sonucu: tarayıcı ailesi (örn. Chrome).</summary>
    public string? BrowserName { get; set; }
    /// <summary>Tarayıcı sürümü.</summary>
    public string? BrowserVersion { get; set; }
    /// <summary>İşletim sistemi ailesi (örn. Windows).</summary>
    public string? OperatingSystem { get; set; }
    /// <summary>Cihaz tipi (Desktop / Mobile / Tablet / Other).</summary>
    public string? DeviceType { get; set; }
    /// <summary>HTTP Accept-Language başlığı.</summary>
    public string? AcceptLanguage { get; set; }
    /// <summary>HTTP Referer başlığı.</summary>
    public string? Referer { get; set; }
    /// <summary>GeoIP — ülke kodu (Faz 1+'da doldurulacak; şu an null).</summary>
    public string? Country { get; set; }
    /// <summary>GeoIP — şehir (Faz 1+'da doldurulacak; şu an null).</summary>
    public string? City { get; set; }

    // ── Request bağlamı ──
    /// <summary>W3C TraceContext trace-id (HTTP istek).</summary>
    public string? TraceId { get; set; }
    /// <summary>Application-level correlation id (varsa).</summary>
    public string? CorrelationId { get; set; }
    /// <summary>HTTP request path (örn. /api/v1/auth/login).</summary>
    public string? RequestPath { get; set; }
    /// <summary>HTTP method (GET/POST/...).</summary>
    public string? RequestMethod { get; set; }

    // ── Environment ──
    /// <summary>Çalışma ortamı (Development/Test/Demo/Production).</summary>
    public string? EnvironmentName { get; set; }
    /// <summary>Sunucu hostname.</summary>
    public string? MachineName { get; set; }
    /// <summary>Application adı (örn. CleanTenant.WebApi).</summary>
    public string? ApplicationName { get; set; }
    /// <summary>Application sürümü (assembly version).</summary>
    public string? ApplicationVersion { get; set; }
    /// <summary>OS process kimliği.</summary>
    public int ProcessId { get; set; }
    /// <summary>İşlemi yapan thread kimliği.</summary>
    public int ThreadId { get; set; }

    // ── Değişiklik ──
    /// <summary>Etkilenen entity'nin kısa sınıf adı (örn. <c>User</c>, <c>Tenant</c>).</summary>
    public string EntityType { get; set; } = string.Empty;
    /// <summary>Etkilenen entity kimliği.</summary>
    public Guid EntityId { get; set; }
    /// <summary>İşlem tipi.</summary>
    public AuditAction Action { get; set; }
    /// <summary>
    /// Delta JSON: <c>{ "Name": { "old": "X", "new": "Y" } }</c>. PII alanları
    /// <c>"[REDACTED]"</c> olarak yazılır.
    /// </summary>
    public string ChangesJson { get; set; } = "{}";
}
