namespace CleanTenant.Application.Common.Auditing;

/// <summary>
/// Aktif istek için <see cref="AuditMetadata"/> üreten sözleşme. HTTP scope'unda
/// <c>HttpAuditMetadataAccessor</c> implement eder; HttpContext + ICurrentSessionAccessor
/// + IHostEnvironment + UAParser'ı birleştirir.
/// </summary>
public interface IAuditMetadataAccessor
{
    /// <summary>Şu anki bağlamı kullanarak yeni bir metadata snapshot'ı üretir.</summary>
    AuditMetadata Capture();
}
