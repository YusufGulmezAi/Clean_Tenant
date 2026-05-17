namespace CleanTenant.Application.Features.System.RevokeSession;

/// <summary>
/// System operatörü tarafından belirli bir Redis session'ı revoke etme isteği.
/// </summary>
/// <param name="SessionId">Hedef session'ın Redis ID'si.</param>
/// <param name="Reason">Zorunlu sebep (min 20 karakter); audit'e işlenir.</param>
public sealed record RevokeSessionCommand(
    Guid SessionId,
    string Reason);
