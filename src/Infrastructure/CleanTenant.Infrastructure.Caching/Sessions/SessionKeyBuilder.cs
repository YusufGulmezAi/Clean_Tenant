using CleanTenant.Application.Common.Auth;
using Microsoft.Extensions.Options;

namespace CleanTenant.Infrastructure.Caching.Sessions;

/// <summary>
/// <para>
/// Redis anahtarlarını standartlaştıran helper. Tüm session anahtarları
/// <c>{prefix}:session:{sessionId}</c> formatında, kullanıcı index'i ise
/// <c>{prefix}:user:{userId}:sessions</c> formatında.
/// </para>
/// </summary>
public sealed class SessionKeyBuilder
{
    private readonly string _prefix;

    /// <summary>DI'dan SessionSettings'i alır.</summary>
    public SessionKeyBuilder(IOptions<SessionSettings> options)
    {
        _prefix = options.Value.KeyPrefix;
    }

    /// <summary>Tek session kaydı için Redis anahtarı.</summary>
    public string SessionKey(Guid sessionId) =>
        $"{_prefix}:session:{sessionId:N}";

    /// <summary>Kullanıcının tüm aktif session id'lerini taşıyan set anahtarı.</summary>
    public string UserSessionsKey(Guid userId) =>
        $"{_prefix}:user:{userId:N}:sessions";
}
