namespace CleanTenant.Application.Common.Jobs;

/// <summary>
/// Arka plan job'ları gibi insan-olmayan aktörler için sabit kimlik. Audit alanları
/// (CreatedBy / GeneratedBy) bu id ile işaretlenir; FK yoktur.
/// </summary>
public static class SystemActor
{
    /// <summary>Sistem/Hangfire job aktörünün sabit kullanıcı id'si.</summary>
    public static readonly Guid UserId = new("00000000-0000-0000-0000-00000000b0b5");
}
