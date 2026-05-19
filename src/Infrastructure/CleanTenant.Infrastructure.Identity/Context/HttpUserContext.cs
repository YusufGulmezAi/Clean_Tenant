using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Identity.Context;

/// <summary>
/// <para>
/// HTTP isteğindeki aktif auth session'ı <see cref="IUserContext"/> arabirimine
/// köprüleyen scoped implementasyon. Normal HTTP request'lerinde
/// <c>SessionLookupMiddleware</c> tarafından Redis'ten yüklenip
/// <see cref="Current"/> setter'ı ile doldurulur.
/// </para>
/// <para>
/// <b>Blazor Server SignalR akışı:</b> MediatR pipeline yeni bir scope'ta
/// çalıştığında middleware tetiklenmez; bu durumda <c>SessionLoaderBehavior</c>
/// (Infrastructure.Identity'de pipeline başı) HttpContext claim'inden async
/// olarak yükler ve setter ile doldurur. Bu yaklaşım sync-over-async'ten
/// kaçınır (Blazor Server SynchronizationContext'inde deadlock yapar).
/// </para>
/// </summary>
public sealed class HttpUserContext : IUserContext, ICurrentSessionAccessor
{
    /// <summary>Mevcut isteğin auth session'ı; doğrulanmamış istek için null.</summary>
    public AuthSession? Current { get; set; }

    /// <inheritdoc />
    public Guid? UserId => Current?.UserId;

    /// <inheritdoc />
    public string? UserName => Current?.UserName;

    /// <inheritdoc />
    public string? Email => Current?.Email;

    /// <inheritdoc />
    public bool IsAuthenticated => Current is not null;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles => Current?.Roles ?? [];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions => Current?.Permissions ?? [];
}
