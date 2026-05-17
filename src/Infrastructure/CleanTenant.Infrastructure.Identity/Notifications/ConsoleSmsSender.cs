using CleanTenant.Application.Common.Notifications;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Identity.Notifications;

/// <summary>
/// Development / Test ortamı için SMS sender — gerçek SMS sağlayıcısı yerine
/// log'a yazar. Production'da kayıt edilmez (boot'ta guard hatası).
/// </summary>
internal sealed class ConsoleSmsSender : ISmsSender
{
    private readonly ILogger<ConsoleSmsSender> _logger;

    public ConsoleSmsSender(ILogger<ConsoleSmsSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[ConsoleSmsSender] To: {ToPhone} | Message: {Message}",
            toPhone, message);
        return Task.CompletedTask;
    }
}
