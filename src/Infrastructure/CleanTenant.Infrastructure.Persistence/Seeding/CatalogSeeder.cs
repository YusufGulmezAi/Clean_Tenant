using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Infrastructure.Persistence.Seeding;

/// <summary>
/// <para>
/// Catalog DB'ye temel verileri seed eden orchestrator. Permission kataloğunu
/// ve built-in rolleri her ortamda (Dev/Test/Demo/Production) idempotent
/// olarak yükler. Ortama özel ek veri (admin kullanıcı, demo tenant) ayrı
/// seeder'larca yapılır (<see cref="DevSeedData"/>, <see cref="DemoSeedData"/>).
/// </para>
/// <para>
/// <b>Idempotency:</b> Her run'da mevcut kayıtlar kontrol edilir; yoksa eklenir.
/// Mevcutsa atlanır. Yeniden çalıştırmak güvenli.
/// </para>
/// </summary>
public sealed class CatalogSeeder
{
    private readonly CatalogDbContext _db;
    private readonly RoleManager<Role> _roleManager;
    private readonly ILogger<CatalogSeeder> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CatalogSeeder(
        CatalogDbContext db,
        RoleManager<Role> roleManager,
        ILogger<CatalogSeeder> logger)
    {
        _db = db;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Permission kataloğunu ve built-in rolleri tüm ortamlarda idempotent
    /// olarak seed eder.
    /// </summary>
    public async Task SeedCoreCatalogAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedBuiltInRolesAsync(cancellationToken);
        await SeedDeveloperPermissionsAsync(cancellationToken);
        await SeedSystemAdminPermissionsAsync(cancellationToken);
    }

    // SystemAdmin built-in rolüne her deployment'ta otomatik atanması gereken
    // baseline izinler. Developer "tam erişim"den farklı olarak SystemAdmin
    // yalnız sistem yönetim sorumluluğuna giren izinleri alır; kalan System
    // izinleri (Support Mode, Impersonate vb.) operatörce elle verilir.
    private static readonly string[] SystemAdminBaselinePermissions =
    [
        "System.Localization.Manage",
        "System.Users.Manage",
        "LookUp.Manage",
    ];

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        // Mevcut permission'ları tracked olarak çek; hem ekleme (yeni kod) hem
        // güncelleme (Description/Module/MinimumRoleScope değişmişse) için.
        var existing = await _db.Permissions
            .ToDictionaryAsync(p => p.Code, p => p, StringComparer.Ordinal, cancellationToken);

        var added = 0;
        var updated = 0;
        foreach (var def in PermissionCatalog.All)
        {
            if (existing.TryGetValue(def.Code, out var current))
            {
                // Var olan kayıt; katalog metadata'sına göre senkronla.
                var dirty = false;
                if (current.Description != def.Description) { current.Description = def.Description; dirty = true; }
                if (current.Module != def.Module) { current.Module = def.Module; dirty = true; }
                if (current.MinimumRoleScope != def.MinimumRoleScope) { current.MinimumRoleScope = def.MinimumRoleScope; dirty = true; }
                if (dirty) updated++;
                continue;
            }

            // Yeni kayıt — Id explicit set; toplu Add sırasında Guid.Empty'leri EF
            // duplicate gibi yorumlamasını engeller (BaseEntity.Id protected setter'lı,
            // o yüzden EF tracker entry üzerinden atıyoruz).
            var entry = _db.Permissions.Add(new Permission
            {
                Code = def.Code,
                Description = def.Description,
                Module = def.Module,
                MinimumRoleScope = def.MinimumRoleScope,
            });
            entry.Property(nameof(Permission.Id)).CurrentValue = Guid.CreateVersion7();
            added++;
        }

        if (added > 0 || updated > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Permission seed: {Added} eklendi, {Updated} güncellendi (toplam {Total}).",
                added, updated, PermissionCatalog.All.Count);
        }
        else
        {
            _logger.LogInformation("Permission seed: değişiklik yok ({Total} kayıt zaten mevcut).", PermissionCatalog.All.Count);
        }
    }

    private async Task SeedBuiltInRolesAsync(CancellationToken cancellationToken)
    {
        var added = 0;
        foreach (var def in BuiltInRoleCatalog.All)
        {
            // RoleManager'ın FindByName'i NormalizedName üzerinden case-insensitive arar.
            // Ama bizim "(NormalizedName, Scope)" bileşik unique olduğundan, scope'lu manuel kontrol.
            var existsForScope = await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.NormalizedName == def.Name.ToUpperInvariant() && r.Scope == def.Scope, cancellationToken);

            if (existsForScope)
            {
                continue;
            }

            var role = new Role
            {
                Name = def.Name,
                NormalizedName = def.Name.ToUpperInvariant(),
                Description = def.Description,
                Scope = def.Scope,
                IsBuiltIn = true,
            };

            // RoleManager.CreateAsync kendi başına SaveChanges'i çağırır.
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Built-in rol oluşturulamadı: {def.Name} ({def.Scope}). Hatalar: "
                    + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            added++;
        }

        if (added > 0)
        {
            _logger.LogInformation("Built-in rol seed: {Added} yeni kayıt eklendi (toplam {Total}).", added, BuiltInRoleCatalog.All.Count);
        }
        else
        {
            _logger.LogInformation("Built-in rol seed: değişiklik yok ({Total} kayıt zaten mevcut).", BuiltInRoleCatalog.All.Count);
        }
    }

    /// <summary>
    /// Developer rolüne katalogdaki tüm permission'ları idempotent olarak atar.
    /// Developer "tam erişim" rolüdür; yeni permission eklenince otomatik kazanır.
    /// </summary>
    private async Task SeedDeveloperPermissionsAsync(CancellationToken cancellationToken)
    {
        var developerRole = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == "DEVELOPER", cancellationToken);

        if (developerRole is null)
        {
            _logger.LogWarning("Developer rolü bulunamadı; permission seed atlandı.");
            return;
        }

        var allPermissions = await _db.Permissions
            .AsNoTracking()
            .Select(p => new { p.Id })
            .ToListAsync(cancellationToken);

        var existingAssignments = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == developerRole.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(cancellationToken);

        var added = 0;
        foreach (var permission in allPermissions)
        {
            if (existingAssignments.Contains(permission.Id))
                continue;

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = developerRole.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = null,
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Developer rol permission seed: {Added} yeni atama yapıldı.", added);
        }
        else
        {
            _logger.LogInformation("Developer rol permission seed: değişiklik yok.");
        }
    }

    /// <summary>
    /// SystemAdmin built-in rolüne <see cref="SystemAdminBaselinePermissions"/>
    /// listesindeki izinleri idempotent atar. Yeni baseline izinler eklenince
    /// otomatik kazanılır; mevcut atamalar korunur.
    /// </summary>
    private async Task SeedSystemAdminPermissionsAsync(CancellationToken cancellationToken)
    {
        if (SystemAdminBaselinePermissions.Length == 0)
        {
            return;
        }

        var systemAdminRole = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == "SYSTEMADMIN", cancellationToken);

        if (systemAdminRole is null)
        {
            _logger.LogWarning("SystemAdmin rolü bulunamadı; baseline permission seed atlandı.");
            return;
        }

        var baselinePermissionIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => SystemAdminBaselinePermissions.Contains(p.Code))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var existingAssignments = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == systemAdminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(cancellationToken);

        var added = 0;
        foreach (var permissionId in baselinePermissionIds)
        {
            if (existingAssignments.Contains(permissionId))
            {
                continue;
            }

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = systemAdminRole.Id,
                PermissionId = permissionId,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = null,
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("SystemAdmin baseline permission seed: {Added} yeni atama yapıldı.", added);
        }
        else
        {
            _logger.LogInformation("SystemAdmin baseline permission seed: değişiklik yok.");
        }
    }
}
