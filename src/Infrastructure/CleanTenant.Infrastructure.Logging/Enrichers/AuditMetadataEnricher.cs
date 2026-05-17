using CleanTenant.Application.Common.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace CleanTenant.Infrastructure.Logging.Enrichers;

/// <summary>
/// <para>
/// Her log event'ine kullanıcı + lokasyon + request + environment metadata'sını
/// property olarak ekler. HTTP scope dışında çağrılırsa (örn. background job,
/// startup) sadece environment alanları doldurulur.
/// </para>
/// <para>
/// Implementasyon: Serilog enricher'ları singleton; <see cref="IAuditMetadataAccessor"/>
/// scoped olduğu için <c>HttpContext.RequestServices</c> üzerinden resolve edilir.
/// </para>
/// </summary>
internal sealed class AuditMetadataEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuditMetadataEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var http = _httpContextAccessor.HttpContext;
        var accessor = http?.RequestServices.GetService<IAuditMetadataAccessor>();
        if (accessor is null)
        {
            return; // HTTP scope dışı — Serilog default enricher'lar yeter.
        }

        var meta = accessor.Capture();

        AddIfNotNull(logEvent, propertyFactory, "UserId", meta.UserId);
        AddIfNotNull(logEvent, propertyFactory, "UserEmail", meta.UserEmail);
        AddIfNotNull(logEvent, propertyFactory, "UserFullName", meta.UserFullName);
        AddIfNotNull(logEvent, propertyFactory, "TenantId", meta.TenantId);
        AddIfNotNull(logEvent, propertyFactory, "TenantName", meta.TenantName);
        AddIfNotNull(logEvent, propertyFactory, "ScopeLevel", meta.ScopeLevel);
        AddIfNotNull(logEvent, propertyFactory, "CompanyId", meta.CompanyId);
        AddIfNotNull(logEvent, propertyFactory, "UnitId", meta.UnitId);
        AddIfNotNull(logEvent, propertyFactory, "PersonaSide", meta.PersonaSide);
        if (meta.Roles.Count > 0)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Roles", meta.Roles, destructureObjects: true));
        }
        if (meta.IsSystemSession)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IsSystemSession", true));
        }
        AddIfNotNull(logEvent, propertyFactory, "SupportSessionId", meta.SupportSessionId);
        AddIfNotNull(logEvent, propertyFactory, "ImpersonatedByUserId", meta.ImpersonatedByUserId);

        AddIfNotNull(logEvent, propertyFactory, "IpAddress", meta.IpAddress);
        AddIfNotNull(logEvent, propertyFactory, "UserAgent", meta.UserAgent);
        AddIfNotNull(logEvent, propertyFactory, "BrowserName", meta.BrowserName);
        AddIfNotNull(logEvent, propertyFactory, "BrowserVersion", meta.BrowserVersion);
        AddIfNotNull(logEvent, propertyFactory, "OperatingSystem", meta.OperatingSystem);
        AddIfNotNull(logEvent, propertyFactory, "DeviceType", meta.DeviceType);
        AddIfNotNull(logEvent, propertyFactory, "AcceptLanguage", meta.AcceptLanguage);
        AddIfNotNull(logEvent, propertyFactory, "Referer", meta.Referer);

        AddIfNotNull(logEvent, propertyFactory, "TraceIdRequest", meta.TraceId);
        AddIfNotNull(logEvent, propertyFactory, "CorrelationId", meta.CorrelationId);
        AddIfNotNull(logEvent, propertyFactory, "RequestPath", meta.RequestPath);
        AddIfNotNull(logEvent, propertyFactory, "RequestMethod", meta.RequestMethod);

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("EnvironmentName", meta.EnvironmentName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationName", meta.ApplicationName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationVersion", meta.ApplicationVersion));
    }

    private static void AddIfNotNull(
        LogEvent logEvent,
        ILogEventPropertyFactory propertyFactory,
        string name,
        object? value)
    {
        if (value is null)
        {
            return;
        }
        if (value is string s && string.IsNullOrEmpty(s))
        {
            return;
        }
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(name, value));
    }
}
