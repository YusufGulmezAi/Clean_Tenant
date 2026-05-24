using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Common.Auth;

/// <summary>
/// <see cref="IAccountLockoutService"/>'in Catalog DB + Identity implementasyonu.
/// Politika çözümlemesi login anında (tenant context seçilmeden) yapıldığı için
/// kullanıcının tenant'ı <c>UserRoleAssignment</c>'lardan bulunur ve ayar
/// Catalog <c>tenants</c> tablosundan okunur.
/// </summary>
public sealed class AccountLockoutService : IAccountLockoutService
{
    private readonly UserManager<User> _userManager;
    private readonly ICatalogDbContext _db;
    private readonly IClock _clock;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AccountLockoutService(UserManager<User> userManager, ICatalogDbContext db, IClock clock)
    {
        _userManager = userManager;
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<LockoutPolicy> ResolvePolicyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Kullanıcının aktif tenant-scope atamalarındaki ayrık tenant'ların
        // kilitleme ayarlarını oku (TimeSpan.FromMinutes SQL'e çevrilemediği için
        // ham kolonları çekip bellekte LockoutPolicy'ye map'liyoruz).
        var rows = await _db.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive && a.TenantId != null)
            .Select(a => a.TenantId!.Value)
            .Distinct()
            .Join(_db.Tenants, id => id, t => t.Id, (_, t) => new
            {
                t.LockoutEnabled,
                t.LockoutMaxFailedAttempts,
                t.LockoutDurationMinutes,
            })
            .ToListAsync(cancellationToken);

        var policies = rows
            .Select(r => new LockoutPolicy(
                r.LockoutEnabled,
                r.LockoutMaxFailedAttempts,
                TimeSpan.FromMinutes(r.LockoutDurationMinutes)))
            .ToList();

        return LockoutDecision.SelectEffective(policies);
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> RegisterFailedAttemptAsync(User user, CancellationToken cancellationToken = default)
    {
        var policy = await ResolvePolicyAsync(user.Id, cancellationToken);

        // Kilitleme kapalıysa sayaç bile tutmaya gerek yok — hesap hiç kilitlenmez.
        if (!policy.Enabled)
        {
            return null;
        }

        await _userManager.AccessFailedAsync(user);

        var failedCount = await _userManager.GetAccessFailedCountAsync(user);
        if (!LockoutDecision.ShouldLock(policy, failedCount))
        {
            return null;
        }

        // Eşik aşıldı → kilitle. Sayacı sıfırla ki kilit sonrası yeniden tam hak tanınsın
        // (ASP.NET Identity'nin yerleşik otomatik kilidiyle aynı davranış).
        var lockedUntil = _clock.UtcNow.Add(policy.Duration);
        await _userManager.SetLockoutEndDateAsync(user, lockedUntil);
        await _userManager.ResetAccessFailedCountAsync(user);
        return lockedUntil;
    }
}
