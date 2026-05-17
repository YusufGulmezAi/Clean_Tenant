using System.Reflection;
using CleanTenant.Application.Common.Pipeline;
using FluentValidation;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace CleanTenant.Application;

/// <summary>
/// Application katmanının DI extension'ı. MediatR + validator'lar + pipeline
/// behavior'ları tek satırla composition root'a bağlar.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// MediatR'ı, FluentValidation validator'larını ve pipeline behavior'ları
    /// kayıt eder. Behavior sırası önemlidir:
    /// <list type="number">
    ///   <item><c>AuthorizationBehavior</c> — yetkisiz çağrıyı erkenden reddet.</item>
    ///   <item><c>ValidationBehavior</c> — input formatını kontrol et.</item>
    ///   <item><c>LoggingBehavior</c> — handler etrafında timing logla.</item>
    /// </list>
    /// MediatR <c>AddTransient</c> ile kayıt edilenleri sırasıyla zincirler.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        // MediatR — tüm IRequestHandler'lar otomatik kayıt edilir
        services.AddMediatR(applicationAssembly);

        // FluentValidation — tüm IValidator<>'lar otomatik kayıt edilir
        services.AddValidatorsFromAssembly(applicationAssembly);

        // Pipeline behavior sırası (önemli):
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
