namespace CleanTenant.Domain.Auditing;

/// <summary>
/// Property üzerine konulduğunda <c>FullAuditInterceptor</c> tarafından
/// değerin yerine <c>"[REDACTED]"</c> yazılır. PII / şifre hash / security
/// stamp gibi alanlar için.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SensitiveAttribute : Attribute;
