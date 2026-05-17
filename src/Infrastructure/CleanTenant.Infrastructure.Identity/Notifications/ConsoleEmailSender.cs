using CleanTenant.Application.Common.Notifications;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Identity.Notifications;

/// <summary>
/// Development / Test ortamı için e-posta sender — gerçek SMTP yerine
/// log'a yazar. Production'da kayıt edilmez (boot'ta guard hatası).
/// </summary>
internal sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[ConsoleEmailSender] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
