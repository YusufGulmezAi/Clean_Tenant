using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Context;
using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.BackgroundJobs.Dashboard;

/// <summary>
/// <c>/hangfire</c> dashboard erişim filtresi — yalnız aktif oturumu System scope
/// olan kullanıcı (SistemAdmin) görebilir (kullanıcı kararı 2026-05-24).
/// </summary>
public sealed class SystemScopeDashboardFilter : IDashboardAuthorizationFilter
{
    /// <inheritdoc />
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var session = http?.RequestServices.GetService<ICurrentSessionAccessor>();
        return session?.Current?.ScopeLevel == ScopeLevel.System;
    }
}
