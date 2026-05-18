using System.Reflection;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// <para>
/// Pipeline'ın <b>en başında</b> çalışır. İki kontrol yapar:
/// </para>
/// <list type="number">
///   <item>
///     <b>Permission kontrolü:</b> Komut/sorgu tipindeki
///     <see cref="RequirePermissionAttribute"/> okunur; aktif oturumun
///     permission'ları yetmiyorsa <c>AUTH-PERMISSION-DENIED</c> ile failure
///     döner — handler hiç çalışmaz, validation hatası gibi bilgi sızdırılmaz.
///   </item>
///   <item>
///     <b>Support Mode write guard (v0.2.3.c):</b> Sistem operatörü destek
///     bağlamında ve <c>SupportMode = "ReadOnly"</c> ise, opt-in
///     <see cref="TenantWriteOperationAttribute"/> taşıyan komutlar reddedilir
///     (<c>AUTH-SUPPORT-WRITE-DENIED</c>). Oturum hareketi komutları (SwitchTenant,
///     Logout, Refresh, 2FA) marker taşımaz; her zaman geçer.
///   </item>
/// </list>
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuthorizationBehavior(
        IPermissionChecker permissionChecker,
        ICurrentSessionAccessor sessionAccessor)
    {
        _permissionChecker = permissionChecker;
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);

        // 1. Permission kontrolü
        var requirement = requestType.GetCustomAttribute<RequirePermissionAttribute>(inherit: false);
        if (requirement is not null && !_permissionChecker.HasAnyPermission(requirement.Permissions))
        {
            var error = Error.Forbidden(
                "AUTH-PERMISSION-DENIED",
                $"Bu işlem için gerekli yetkilere sahip değilsiniz: {string.Join(", ", requirement.Permissions)}.");
            return Task.FromResult(ResultFactoryHelper.CreateFailure<TResponse>([error]));
        }

        // 2. Support Mode write guard: opt-in [TenantWriteOperation] komutları
        // Sistem operatör + ReadOnly destek modunda iken bloklanır.
        if (requestType.GetCustomAttribute<TenantWriteOperationAttribute>(inherit: false) is not null)
        {
            var session = _sessionAccessor.Current;
            if (session is not null
                && session.IsSystemSession
                && string.Equals(session.SupportMode, "ReadOnly", StringComparison.Ordinal))
            {
                var error = Error.Forbidden(
                    "AUTH-SUPPORT-WRITE-DENIED",
                    "Bu Yönetim'de Sistem destek yazma yetkisi kapalı. YönetimAdmin'den izin isteyin.");
                return Task.FromResult(ResultFactoryHelper.CreateFailure<TResponse>([error]));
            }
        }

        return next();
    }
}
