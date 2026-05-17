using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

/// <summary>
/// PostgreSQL'in <c>unaccent(lower(...))</c> davranışı ile .NET tarafındaki
/// <see cref="TurkishStringNormalizer.Normalize"/> çıktısının birebir hizalı
/// olduğunu doğrular. Sapma olursa kullanıcının UI arama sonucu DB sonucundan
/// farklı çıkar.
/// </summary>
public sealed class TurkishSearchAlignmentTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public TurkishSearchAlignmentTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("İSTANBUL")]
    [InlineData("Şişli")]
    [InlineData("Çankaya")]
    [InlineData("Ğümüşhane")]
    [InlineData("Öztürk")]
    [InlineData("Üsküdar")]
    [InlineData("Ahmet")]
    public async Task PG_unaccent_lower_dotnet_normalize_ile_ayni_sonucu_uretir(string input)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync();
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT unaccent(lower(@input))";
        var parameter = cmd.CreateParameter();
        parameter.ParameterName = "@input";
        parameter.Value = input;
        cmd.Parameters.Add(parameter);

        var pgResult = (string)(await cmd.ExecuteScalarAsync())!;
        var netResult = TurkishStringNormalizer.Normalize(input);

        pgResult.Should().Be(netResult,
            because: $"PostgreSQL ve .NET '{input}' için aynı normalize çıktısını üretmeli");
    }
}
