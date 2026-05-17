using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CleanTenant.Infrastructure.Persistence.Audit;

/// <summary>
/// EF Core CLI'nın migration üretirken kullandığı design-time factory.
/// Audit DB için varsayılan local bağlantı.
/// </summary>
internal sealed class AuditDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    /// <inheritdoc />
    public AuditDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Audit")
            ?? "Host=localhost;Port=5432;Database=cleantenant_audit;Username=cleantenant;Password=cleantenant_dev_only";

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(AuditDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuditDbContext(options);
    }
}
