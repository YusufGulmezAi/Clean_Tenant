using Microsoft.AspNetCore.Authorization;

namespace CleanTenant.Infrastructure.Identity.Authorization;

/// <summary>System scope gereksinimi.</summary>
public sealed class SystemScopeRequirement : IAuthorizationRequirement;

/// <summary>Tenant scope gereksinimi (Tenant / Company / Unit).</summary>
public sealed class TenantScopeRequirement : IAuthorizationRequirement;

/// <summary>Aktif Support Mode gereksinimi (ReadOnly / WriteEnabled / FullImpersonation).</summary>
public sealed class SupportModeActiveRequirement : IAuthorizationRequirement;

/// <summary>WriteEnabled veya FullImpersonation Support Mode gereksinimi.</summary>
public sealed class SupportWriteEnabledRequirement : IAuthorizationRequirement;
