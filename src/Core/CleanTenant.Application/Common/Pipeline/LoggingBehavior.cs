using System.Diagnostics;
using CleanTenant.Application.Common.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// <para>
/// Validation sonrası, handler etrafında çalışır. Her request için
/// <c>Information</c> seviyede başlangıç, başarı/başarısızlık ve geçen
/// milisaniye loglar. <b>Payload loglanmaz</b> — PII riski; v0.1.7 audit
/// interceptor'la birlikte PII-aware audit yapılır.
/// </para>
/// <para>
/// Anonim çağrılarda (challenge token yoluyla 2FA verify gibi) <c>UserId</c>
/// alanı <c>anonymous</c> olarak loglanır.
/// </para>
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentSessionAccessor sessionAccessor)
    {
        _logger = logger;
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _sessionAccessor.Current?.UserId.ToString("N") ?? "anonymous";

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation(
                "MediatR {Request} user={UserId} elapsed={ElapsedMs}ms",
                requestName, userId, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "MediatR {Request} user={UserId} faulted elapsed={ElapsedMs}ms",
                requestName, userId, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
