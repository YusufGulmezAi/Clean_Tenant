using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CleanTenant.Infrastructure.Persistence.Catalog;

/// <summary>
/// <para>
/// <c>dotnet ef</c> CLI komutları (migration üretme, database update vb.)
/// için <see cref="CatalogDbContext"/>'in design-time fabrikasıdır.
/// </para>
/// <para>
/// EF Core CLI runtime'da WebApi composition root çalışmadığı için DI'dan
/// DbContext alamaz. Bu fabrika sayesinde CLI, connection string'i ortam
/// değişkenlerinden okuyup DbContext'i oluşturabilir.
/// </para>
/// <para>
/// <b>Connection string kaynağı:</b> <c>ConnectionStrings__Catalog</c> ortam
/// değişkeni. Boşsa design-time placeholder kullanır — migration üretmek için
/// gerçek DB'ye bağlanılmaz; yalnız database update için bağlantı zorunlu.
/// </para>
/// </summary>
internal sealed class CatalogDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    /// <inheritdoc />
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Catalog")
            ?? "Host=localhost;Port=5432;Database=cleantenant_catalog;Username=design;Password=design";

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new CatalogDbContext(options);
    }
}
