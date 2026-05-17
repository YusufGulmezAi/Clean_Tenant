using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanTenant.Infrastructure.Persistence.Log;

/// <summary>EF CLI design-time factory for Log DB.</summary>
internal sealed class LogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LogDbContext>
{
    /// <inheritdoc />
    public LogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Log")
            ?? "Host=localhost;Port=5432;Database=cleantenant_log;Username=cleantenant;Password=cleantenant_dev_only";

        var options = new DbContextOptionsBuilder<LogDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsAssembly(typeof(LogDbContext).Assembly.GetName().Name))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new LogDbContext(options);
    }
}
