using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Support;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanTenant.Application.Features.System.ElevateToWrite;

/// <summary>
/// Mevcut Support Mode session'ını WriteEnabled'a yükseltir.
/// JWT yenilenmiyor — istemci aynı access token'la devam eder.
/// Redis session in-place mutate edilir, <c>SupportSession.Mode</c> DB'de update.
/// </summary>
public sealed class ElevateToWriteCommandHandler
{
    private readonly ICatalogDbContext _db;
    private readonly IAuthSessionStore _sessionStore;
    private readonly ICurrentSessionAccessor _sessionAccessor;
    private readonly IClock _clock;
    private readonly SessionSettings _sessionSettings;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ElevateToWriteCommandHandler(
        ICatalogDbContext db,
        IAuthSessionStore sessionStore,
        ICurrentSessionAccessor sessionAccessor,
        IClock clock,
        IOptions<SessionSettings> sessionOptions)
    {
        _db = db;
        _sessionStore = sessionStore;
        _sessionAccessor = sessionAccessor;
        _clock = clock;
        _sessionSettings = sessionOptions.Value;
    }

    /// <summary>Elevate-to-write işlemini uygular.</summary>
    public async Task<Result> HandleAsync(ElevateToWriteCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Length < 20)
        {
            return Result.Failure(
                Error.Validation("SUP-005", "Sebep zorunlu (minimum 20 karakter)."));
        }

        var current = _sessionAccessor.Current
            ?? throw new InvalidOperationException("ICurrentSessionAccessor.Current null.");

        if (current.SupportSessionId is null || current.SupportMode != "ReadOnly")
        {
            return Result.Failure(
                Error.Failure("SUP-006", "ReadOnly Support Mode oturumu gerekli."));
        }

        // SupportSession.Mode DB güncellenir
        var support = await _db.SupportSessions
            .FirstOrDefaultAsync(s => s.Id == current.SupportSessionId.Value, cancellationToken);
        if (support is null)
        {
            return Result.Failure(
                Error.NotFound("SUP-007", "SupportSession DB kaydı bulunamadı."));
        }
        support.Mode = SupportSessionMode.WriteEnabled;
        await _db.SaveChangesAsync(cancellationToken);

        // Redis session in-place mutate
        current.SupportMode = "WriteEnabled";
        current.LastActivity = _clock.UtcNow;
        var ttl = TimeSpan.FromMinutes(10 + _sessionSettings.TtlPaddingMinutes);
        await _sessionStore.UpdateAsync(current, ttl, cancellationToken);

        return Result.Success();
    }
}
