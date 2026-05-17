using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Authorization;

namespace CleanTenant.Infrastructure.Identity.Authorization;

/// <summary>
/// Ortak base — aktif <see cref="AuthSession"/>'a göre tek bir koşulu test
/// eden authorization handler.
/// </summary>
/// <typeparam name="TRequirement">Test edilecek requirement tipi.</typeparam>
internal abstract class SessionRequirementHandler<TRequirement> : AuthorizationHandler<TRequirement>
    where TRequirement : IAuthorizationRequirement
{
    private readonly ICurrentSessionAccessor _sessionAccessor;

    protected SessionRequirementHandler(ICurrentSessionAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    /// <summary>Türemiş sınıf bu koşulu test eder.</summary>
    protected abstract bool IsSatisfied(AuthSession session);

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TRequirement requirement)
    {
        var session = _sessionAccessor.Current;
        if (session is not null && IsSatisfied(session))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

internal sealed class SystemScopeHandler : SessionRequirementHandler<SystemScopeRequirement>
{
    public SystemScopeHandler(ICurrentSessionAccessor sessionAccessor) : base(sessionAccessor) { }
    protected override bool IsSatisfied(AuthSession session) =>
        session.ScopeLevel == ScopeLevel.System;
}

internal sealed class TenantScopeHandler : SessionRequirementHandler<TenantScopeRequirement>
{
    public TenantScopeHandler(ICurrentSessionAccessor sessionAccessor) : base(sessionAccessor) { }
    protected override bool IsSatisfied(AuthSession session) =>
        session.ScopeLevel is ScopeLevel.Tenant or ScopeLevel.Company or ScopeLevel.Unit;
}

internal sealed class SupportModeActiveHandler : SessionRequirementHandler<SupportModeActiveRequirement>
{
    public SupportModeActiveHandler(ICurrentSessionAccessor sessionAccessor) : base(sessionAccessor) { }
    protected override bool IsSatisfied(AuthSession session) =>
        session.IsSystemSession
        && session.SupportMode is "ReadOnly" or "WriteEnabled" or "FullImpersonation";
}

internal sealed class SupportWriteEnabledHandler : SessionRequirementHandler<SupportWriteEnabledRequirement>
{
    public SupportWriteEnabledHandler(ICurrentSessionAccessor sessionAccessor) : base(sessionAccessor) { }
    protected override bool IsSatisfied(AuthSession session) =>
        session.IsSystemSession
        && session.SupportMode is "WriteEnabled" or "FullImpersonation";
}
