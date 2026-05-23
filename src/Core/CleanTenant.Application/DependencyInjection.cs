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
    ///   <item><c>CachingBehavior</c> (v0.2.3.f) — <see cref="Common.Caching.CacheableAttribute"/> taşıyan Query'leri cache'le.</item>
    ///   <item><c>ValidationBehavior</c> — input formatını kontrol et (cache miss path'inde).</item>
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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Tahakkuk dağıtım servisi (saf hesaplama, LRM yuvarlama)
        services.AddSingleton<Features.Main.Accruals.Distribution.IDistributionService,
            Features.Main.Accruals.Distribution.DistributionService>();

        // Tahakkuk → yevmiye fişi postlayıcı (IMainDbContext kullanır → scoped)
        services.AddScoped<Features.Main.Accruals.Posting.IAccrualJournalPoster,
            Features.Main.Accruals.Posting.AccrualJournalPoster>();

        // Gecikme faizi: saf hesaplama + politika çözümleyici (stateless → singleton)
        services.AddSingleton<Features.Main.LateFees.Calculation.ILateFeeCalculator,
            Features.Main.LateFees.Calculation.LateFeeCalculator>();
        services.AddSingleton<Features.Main.LateFees.Calculation.ILateFeePolicyResolver,
            Features.Main.LateFees.Calculation.LateFeePolicyResolver>();

        // v0.2.13.e — Scope izin çözümleyici (cascade kuralı tek yerde). Scoped:
        // ICatalogDbContext'e bağlı.
        services.AddScoped<Common.Authorization.IScopePermissionResolver,
            Common.Authorization.ScopePermissionResolver>();

        return services;
    }
}
