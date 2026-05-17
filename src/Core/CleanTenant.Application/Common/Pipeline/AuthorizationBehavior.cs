using System.Reflection;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// <para>
/// Pipeline'ın <b>en başında</b> çalışır. Komut/sorgu tipindeki
/// <see cref="RequirePermissionAttribute"/> kontrol edilir; aktif oturumun
/// permission'ları yetmiyorsa <c>AUTH-PERMISSION-DENIED</c> ile failure döner
/// — handler hiç çalışmaz, validation hatası gibi bilgi sızdırılmaz.
/// </para>
/// <para>
/// Attribute yoksa pass-through (handler doğrudan çalışır).
/// </para>
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AuthorizationBehavior(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    /// <inheritdoc />
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requirement = typeof(TRequest).GetCustomAttribute<RequirePermissionAttribute>(inherit: false);
        if (requirement is null)
        {
            return next();
        }

        if (_permissionChecker.HasAnyPermission(requirement.Permissions))
        {
            return next();
        }

        var error = Error.Forbidden(
            "AUTH-PERMISSION-DENIED",
            $"Bu işlem için gerekli yetkilere sahip değilsiniz: {string.Join(", ", requirement.Permissions)}.");
        return Task.FromResult(ResultFactoryHelper.CreateFailure<TResponse>([error]));
    }
}
