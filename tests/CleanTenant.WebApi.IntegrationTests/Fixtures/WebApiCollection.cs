namespace CleanTenant.WebApi.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection tanımı — tüm WebApi integration test sınıfları tek bir
/// <see cref="WebApiFactoryFixture"/> paylaşır. Container'lar tek seferde
/// başlatılır, process env var'ları (connection string'ler vb.) bir kez set
/// edilir; paralel sınıf çakışmaları olmaz.
/// </summary>
[CollectionDefinition(nameof(WebApiCollection), DisableParallelization = true)]
public sealed class WebApiCollection : ICollectionFixture<WebApiFactoryFixture>
{
}
