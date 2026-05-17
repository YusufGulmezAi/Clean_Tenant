using CleanTenant.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CleanTenant.Infrastructure.Persistence.Main;

/// <summary>EF CLI design-time factory for Main DB.</summary>
internal sealed class MainDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    /// <inheritdoc />
    public MainDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Main")
            ?? "Host=localhost;Port=5432;Database=cleantenant_main;Username=cleantenant;Password=cleantenant_dev_only";

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(MainDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        // Design-time için boş tenant context (migration üretiminde filter çalışmaz)
        return new MainDbContext(options, new DesignTimeTenantContext());
    }

    /// <summary>Migration üretimi sırasında kullanılan boş tenant context.</summary>
    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public Guid? CompanyId => null;
        public Guid? UnitId => null;
        public ScopeLevel CurrentScope => ScopeLevel.None;
    }
}
