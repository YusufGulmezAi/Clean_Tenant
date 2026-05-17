using CleanTenant.WebApi.Configuration;
using CleanTenant.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddCleanTenantEnvironmentMappings();
builder.Services.AddCleanTenantApi(builder.Configuration);

var app = builder.Build();

app.UseCleanTenantPipeline();
app.MapCleanTenantEndpoints();

app.Run();

/// <summary>
/// CleanTenant WebApi giriş noktası. Composition root sadece üst seviye
/// bağlam taşır; tüm detaylar <c>Configuration/*Extensions</c> ve
/// <c>Endpoints/*Extensions</c> dosyalarında.
/// Integration test'lerde <c>WebApplicationFactory&lt;Program&gt;</c>
/// kullanılabilmesi için <c>Program</c> partial public yapılır.
/// </summary>
public partial class Program;
