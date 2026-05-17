using System.Diagnostics;
using System.Reflection;
using CleanTenant.Application.Common.Auditing;
using CleanTenant.Application.Common.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using UAParser;

namespace CleanTenant.Infrastructure.Identity.Auditing;

/// <summary>
/// <see cref="IAuditMetadataAccessor"/>'ın HTTP scope implementasyonu.
/// HttpContext + aktif Redis session + IHostEnvironment + UA parser'ı
/// birleştirip her audit/log için zengin bağlam üretir.
/// </summary>
internal sealed class HttpAuditMetadataAccessor : IAuditMetadataAccessor
{
    /// <summary>UAParser default config'i thread-safe; static olarak paylaşılır.</summary>
    private static readonly Parser UserAgentParser = Parser.GetDefault();

    private static readonly string AssemblyVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

    private static readonly string AssemblyName =
        Assembly.GetEntryAssembly()?.GetName().Name ?? "CleanTenant";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IHostEnvironment _environment;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public HttpAuditMetadataAccessor(
        IHttpContextAccessor httpContextAccessor,
        ICurrentSessionAccessor sessionAccessor,
        IHostEnvironment environment)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionAccessor = sessionAccessor;
        _environment = environment;
    }

    /// <inheritdoc />
    public AuditMetadata Capture()
    {
        var http = _httpContextAccessor.HttpContext;
        var session = _sessionAccessor.Current;

        var ipAddress = http?.Connection.RemoteIpAddress?.ToString();
        var userAgent = http?.Request.Headers.UserAgent.ToString();
        var (browserName, browserVersion, os, deviceType) = ParseUserAgent(userAgent);

        return new AuditMetadata
        {
            // Kullanıcı
            UserId = session?.UserId,
            UserEmail = session?.Email,
            UserFullName = session?.FullName,
            TenantId = session?.TenantId,
            TenantName = session?.TenantName,
            ScopeLevel = session?.ScopeLevel.ToString(),
            CompanyId = session?.CompanyId,
            UnitId = session?.UnitId,
            PersonaSide = session?.PersonaSide.ToString(),
            Roles = session?.Roles ?? [],
            IsSystemSession = session?.IsSystemSession ?? false,
            SupportSessionId = session?.SupportSessionId,
            ImpersonatedByUserId = session?.ImpersonatedBy,

            // Lokasyon
            IpAddress = ipAddress,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            BrowserName = browserName,
            BrowserVersion = browserVersion,
            OperatingSystem = os,
            DeviceType = deviceType,
            AcceptLanguage = http?.Request.Headers.AcceptLanguage.ToString() is { Length: > 0 } al ? al : null,
            Referer = http?.Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : null,
            Country = null,  // GeoIP — Faz 1+
            City = null,

            // Request bağlamı
            TraceId = Activity.Current?.TraceId.ToString() ?? http?.TraceIdentifier,
            CorrelationId = http?.Request.Headers["X-Correlation-Id"].ToString() is { Length: > 0 } c ? c : null,
            RequestPath = http?.Request.Path.Value,
            RequestMethod = http?.Request.Method,

            // Environment
            EnvironmentName = _environment.EnvironmentName,
            MachineName = Environment.MachineName,
            ApplicationName = AssemblyName,
            ApplicationVersion = AssemblyVersion,
            ProcessId = Environment.ProcessId,
            ThreadId = Environment.CurrentManagedThreadId,
        };
    }

    /// <summary>UA string'ini parse edip tarayıcı/OS/cihaz tipini döner.</summary>
    private static (string? browser, string? version, string? os, string? device) ParseUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return (null, null, null, null);
        }

        var info = UserAgentParser.Parse(userAgent);
        var browser = info.UA.Family;
        var version = string.Join('.', new[] { info.UA.Major, info.UA.Minor, info.UA.Patch }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
        var os = info.OS.Family;

        // Cihaz tipi — UAParser cihaz family'sini döner, kategorize ediyoruz.
        var deviceFamily = info.Device.Family;
        var deviceType = deviceFamily switch
        {
            "Other" or null => "Desktop",
            _ when info.Device.IsSpider => "Bot",
            _ when deviceFamily.Contains("iPad", StringComparison.OrdinalIgnoreCase) => "Tablet",
            _ when deviceFamily.Contains("iPhone", StringComparison.OrdinalIgnoreCase) => "Mobile",
            _ when deviceFamily.Contains("Tablet", StringComparison.OrdinalIgnoreCase) => "Tablet",
            _ => "Mobile",
        };

        return (browser, string.IsNullOrWhiteSpace(version) ? null : version, os, deviceType);
    }
}
