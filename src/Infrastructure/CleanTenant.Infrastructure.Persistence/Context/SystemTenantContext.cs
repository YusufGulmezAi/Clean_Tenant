using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Infrastructure.Persistence.Context;

/// <summary>
/// "Sistem işlemi" durumunu temsil eden geçici <see cref="ITenantContext"/>
/// implementasyonu. Aktif tenant/company/unit yok; scope = <see cref="ScopeLevel.System"/>.
/// Migration ve seed sırasında DbContext'e enjekte edilir; v0.1.5'te
/// HttpContext-bound versiyonla değiştirilir.
/// </summary>
public sealed class SystemTenantContext : ITenantContext
{
    /// <inheritdoc />
    public Guid? TenantId => null;

    /// <inheritdoc />
    public Guid? CompanyId => null;

    /// <inheritdoc />
    public Guid? UnitId => null;

    /// <inheritdoc />
    public ScopeLevel CurrentScope => ScopeLevel.System;
}
