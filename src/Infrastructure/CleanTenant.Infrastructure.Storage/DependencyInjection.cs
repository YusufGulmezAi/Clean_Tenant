using CleanTenant.Application.Common.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;

namespace CleanTenant.Infrastructure.Storage;

/// <summary>
/// Object storage katmanının DI kayıtları: MinIO client, <see cref="IFileStorage"/>,
/// <see cref="IImageProcessor"/> ve startup bucket bootstrap.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Object storage servislerini kayıt eder.
    /// <list type="bullet">
    ///   <item><c>ObjectStorage:Endpoint</c> doluysa → MinIO (gerçek).</item>
    ///   <item>Boş ve ortam Production değilse → bellek-içi fallback (test/yerel).</item>
    ///   <item>Boş ve ortam Production ise → boot başarısız (MinIO zorunlu).</item>
    /// </list>
    /// <see cref="IImageProcessor"/> her durumda (SkiaSharp) kayıt edilir.
    /// </summary>
    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(ObjectStorageOptions.SectionName);
        services.Configure<ObjectStorageOptions>(section);

        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();

        var options = section.Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            if (environment.IsProduction())
            {
                throw new InvalidOperationException(
                    "ObjectStorage:Endpoint Production ortamında zorunlu — MinIO/S3 yapılandırın.");
            }

            // Test / yerel: MinIO container olmadan boot edebilmek için bellek-içi store.
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();
            return services;
        }

        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build());

        services.AddSingleton<IFileStorage, MinioFileStorage>();
        services.AddHostedService<MinioBucketInitializer>();

        return services;
    }
}
