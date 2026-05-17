using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Persistence.Context;

/// <summary>
/// <para>
/// "Sistem işlemi" durumunu temsil eden geçici <see cref="IUserContext"/>
/// implementasyonu. Tüm alanlar boş döner; audit kayıtlarında <c>CreatedBy</c>
/// gibi alanlar <c>null</c> olarak yazılır — yani "sistem yaptı".
/// </para>
/// <para>
/// <b>Geçici:</b> v0.1.4.b'de placeholder; v0.1.5'te HttpContext-bound
/// <c>HttpUserContext</c> ile (Infrastructure.Identity'de) değiştirilir.
/// Scope'lu DI ile request başına farklı implementasyon enjekte edilir.
/// </para>
/// <para>
/// MigrationRunner ve seed işlemleri her zaman bu implementasyonu görür
/// (HTTP context yok).
/// </para>
/// </summary>
public sealed class SystemUserContext : IUserContext
{
    /// <inheritdoc />
    public Guid? UserId => null;

    /// <inheritdoc />
    public string? UserName => null;

    /// <inheritdoc />
    public string? Email => null;

    /// <inheritdoc />
    public bool IsAuthenticated => false;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles { get; } = [];

    /// <inheritdoc />
    public IReadOnlyCollection<string> Permissions { get; } = [];
}
