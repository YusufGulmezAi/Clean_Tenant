using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// Development ortamı için ek seed verisi. Permission ve built-in rollere ek
/// olarak şu kayıtları idempotent şekilde oluşturur:
/// <list type="bullet">
///   <item>Geliştirici System admin kullanıcı (Yusuf Gülmez)</item>
///   <item>Yusuf'a SystemAdmin (tam erişim) rol ataması (System scope)</item>
/// </list>
/// <para>
/// Demo tenant <b>bilinçli olarak</b> seed edilmez — tenant'lar UI üzerinden
/// (System operatörü) oluşturulur. Soft-delete edilmiş bir demo tenant ile
/// unique index (ix_tenants_name) çakışmasını önlemek için kaldırıldı.
/// </para>
/// </para>
/// <para>
/// <b>Şifre kaynağı:</b> <c>SEED_ADMIN_PASSWORD</c> environment değişkeni
/// (.env.development'tan yüklenir). Şifre policy min 8 + complexity'yi
/// karşılamalıdır; aksi takdirde Identity validation hata verir.
/// </para>
/// </summary>
public sealed class DevSeedData
{
    private const string AdminEmail = "yusuf.gulmez.ai@gmail.com";
    private const string AdminFirstName = "YUSUF";
    private const string AdminLastName = "GÜLMEZ";

    private readonly CatalogDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DevSeedData> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DevSeedData(
        CatalogDbContext db,
        UserManager<User> userManager,
        ILogger<DevSeedData> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>Development seed senaryosunu idempotent şekilde uygular.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var password = Environment.GetEnvironmentVariable("SEED_ADMIN_PASSWORD")
            ?? throw new InvalidOperationException(
                "SEED_ADMIN_PASSWORD environment değişkeni eksik. .env.development içine ekleyin.");

        await EnsureAdminUserAsync(password);
    }

    private async Task EnsureAdminUserAsync(string password)
    {
        var existing = await _userManager.FindByEmailAsync(AdminEmail);
        if (existing is not null)
        {
            _logger.LogInformation("Dev admin kullanıcı zaten mevcut: {Email}", AdminEmail);
            await EnsureSystemAdminRoleAssignmentAsync(existing.Id);
            return;
        }

        var user = new User
        {
            UserName = AdminEmail,
            Email = AdminEmail,
            EmailConfirmed = true,
            FirstName = AdminFirstName,
            LastName = AdminLastName,
            TwoFactorEnabled = false, // Dev'de pratik; production zorunlu olacak
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Dev admin kullanıcı oluşturulamadı. Hatalar: "
                + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Dev admin kullanıcı oluşturuldu: {Email}", AdminEmail);
        await EnsureSystemAdminRoleAssignmentAsync(user.Id);
    }

    private async Task EnsureSystemAdminRoleAssignmentAsync(Guid userId)
    {
        var systemAdminRole = await _db.Roles
            .AsNoTracking()
            .Where(r => r.NormalizedName == "SYSTEMADMIN" && r.Scope == ScopeLevel.System)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (systemAdminRole == Guid.Empty)
        {
            throw new InvalidOperationException(
                "SystemAdmin built-in rolü bulunamadı. SeedCoreCatalogAsync çalıştırılmış olmalı.");
        }

        var alreadyAssigned = await _db.UserRoleAssignments
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userId
                        && a.RoleId == systemAdminRole
                        && a.ScopeLevel == ScopeLevel.System);

        if (alreadyAssigned)
        {
            return;
        }

        _db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = userId,
            RoleId = systemAdminRole,
            ScopeLevel = ScopeLevel.System,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Dev admin'e SystemAdmin (System, tam erişim) rolü atandı.");
    }
}
