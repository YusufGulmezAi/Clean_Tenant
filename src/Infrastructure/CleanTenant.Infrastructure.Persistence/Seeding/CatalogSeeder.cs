using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.LookUp;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.SharedKernel.Context;
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
        await SeedTenantAdminPermissionsAsync(cancellationToken);
        await SeedCompanyAdminPermissionsAsync(cancellationToken);
        await SeedInflationIndexesAsync(cancellationToken);
        await SeedChartOfAccountsTemplatesAsync(cancellationToken);
        await SeedBudgetTypeMetadataAsync(cancellationToken);
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

    /// <summary>
    /// <para>
    /// TenantAdmin built-in rolüne TÜM Tenant- ve Company-scope izinlerini idempotent
    /// atar — "süper tenant kullanıcısı" (v0.2.13.e). System ve Unit izinleri kasıtlı
    /// olarak DIŞARIDA bırakılır (privilege ceiling): <c>Tenant.Create</c>,
    /// <c>Support.*</c> gibi System-only izinler operatöre özeldir; Unit izinleri ise
    /// portal sakin rolleridir.
    /// </para>
    /// <para>
    /// Company izinlerini de içermesi cascade içindir: <c>SwitchTenantCommandHandler</c>
    /// bir siteye geçişte en geniş atamayı (Tenant) baz aldığından, TenantAdmin tenant'ın
    /// tüm sitelerinde tam yetkili olur.
    /// </para>
    /// </summary>
    private async Task SeedTenantAdminPermissionsAsync(CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == "TENANTADMIN" && r.Scope == ScopeLevel.Tenant, cancellationToken);

        if (role is null)
        {
            _logger.LogWarning("TenantAdmin rolü bulunamadı; permission seed atlandı.");
            return;
        }

        var targetPermissionIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Tenant || p.MinimumRoleScope == ScopeLevel.Company)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        await GrantPermissionsToRoleAsync(role.Id, targetPermissionIds, "TenantAdmin", cancellationToken);
    }

    /// <summary>
    /// CompanyAdmin built-in rolüne TÜM Company-scope izinlerini idempotent atar —
    /// "süper company kullanıcısı" (v0.2.13.e). Yalnız <see cref="ScopeLevel.Company"/>
    /// izinleri; Tenant/System/Unit hariç.
    /// </summary>
    private async Task SeedCompanyAdminPermissionsAsync(CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == "COMPANYADMIN" && r.Scope == ScopeLevel.Company, cancellationToken);

        if (role is null)
        {
            _logger.LogWarning("CompanyAdmin rolü bulunamadı; permission seed atlandı.");
            return;
        }

        var targetPermissionIds = await _db.Permissions
            .AsNoTracking()
            .Where(p => p.MinimumRoleScope == ScopeLevel.Company)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        await GrantPermissionsToRoleAsync(role.Id, targetPermissionIds, "CompanyAdmin", cancellationToken);
    }

    /// <summary>
    /// Verilen permission Id setini bir role idempotent atar (mevcut atamalar korunur,
    /// yalnız eksikler eklenir). <see cref="SeedDeveloperPermissionsAsync"/> ile aynı desen.
    /// </summary>
    private async Task GrantPermissionsToRoleAsync(
        Guid roleId,
        IReadOnlyList<Guid> permissionIds,
        string roleLabel,
        CancellationToken cancellationToken)
    {
        var existing = await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToHashSetAsync(cancellationToken);

        var added = 0;
        foreach (var permissionId in permissionIds)
        {
            if (existing.Contains(permissionId))
            {
                continue;
            }

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedBy = null,
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("{Role} permission seed: {Added} yeni atama yapıldı.", roleLabel, added);
        }
        else
        {
            _logger.LogInformation("{Role} permission seed: değişiklik yok.", roleLabel);
        }
    }

    /// <summary>
    /// TÜİK TÜFE enflasyon endeksi verilerini idempotent olarak seed eder.
    /// Tablo doluysa atlanır; yeni yıl verileri eklendikçe mevcut kayıtlar korunur.
    /// </summary>
    private async Task SeedInflationIndexesAsync(CancellationToken cancellationToken)
    {
        // Herhangi bir kayıt varsa seed atla (yılda bir manuel güncelleme beklenir)
        if (await _db.InflationIndexes.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("InflationIndex seed: tabloda kayıt mevcut, seed atlandı.");
            return;
        }

        var indexes = GetInflationIndexes().ToList();
        foreach (var idx in indexes)
        {
            var entry = _db.InflationIndexes.Add(idx);
            entry.Property(nameof(InflationIndex.Id)).CurrentValue = Guid.CreateVersion7();
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("InflationIndex seed: {Count} kayıt eklendi.", indexes.Count);
    }

    /// <summary>2024-2025 TÜİK TÜFE endeks verileri (yaklaşık).</summary>
    private static IEnumerable<InflationIndex> GetInflationIndexes() =>
    [
        // 2024
        new InflationIndex { Year = 2024, Month = 1,  IndexValue = 1743.96m },
        new InflationIndex { Year = 2024, Month = 2,  IndexValue = 1830.72m },
        new InflationIndex { Year = 2024, Month = 3,  IndexValue = 1916.25m },
        new InflationIndex { Year = 2024, Month = 4,  IndexValue = 1987.45m },
        new InflationIndex { Year = 2024, Month = 5,  IndexValue = 2062.18m },
        new InflationIndex { Year = 2024, Month = 6,  IndexValue = 2095.63m },
        new InflationIndex { Year = 2024, Month = 7,  IndexValue = 2126.44m },
        new InflationIndex { Year = 2024, Month = 8,  IndexValue = 2165.82m },
        new InflationIndex { Year = 2024, Month = 9,  IndexValue = 2188.75m },
        new InflationIndex { Year = 2024, Month = 10, IndexValue = 2225.33m },
        new InflationIndex { Year = 2024, Month = 11, IndexValue = 2278.91m },
        new InflationIndex { Year = 2024, Month = 12, IndexValue = 2325.67m },
        // 2025
        new InflationIndex { Year = 2025, Month = 1,  IndexValue = 2389.44m },
        new InflationIndex { Year = 2025, Month = 2,  IndexValue = 2441.22m },
        new InflationIndex { Year = 2025, Month = 3,  IndexValue = 2498.75m },
        new InflationIndex { Year = 2025, Month = 4,  IndexValue = 2542.10m },
        new InflationIndex { Year = 2025, Month = 5,  IndexValue = 2576.33m },
        new InflationIndex { Year = 2025, Month = 6,  IndexValue = 2601.88m },
        new InflationIndex { Year = 2025, Month = 7,  IndexValue = 2634.55m },
        new InflationIndex { Year = 2025, Month = 8,  IndexValue = 2668.92m },
        new InflationIndex { Year = 2025, Month = 9,  IndexValue = 2695.44m },
        new InflationIndex { Year = 2025, Month = 10, IndexValue = 2723.18m },
        new InflationIndex { Year = 2025, Month = 11, IndexValue = 2758.63m },
        new InflationIndex { Year = 2025, Month = 12, IndexValue = 2789.25m },
    ];

    /// <summary>
    /// TDHP hesap planı şablonunu idempotent olarak seed eder.
    /// Tablo doluysa atlanır.
    /// </summary>
    private async Task SeedChartOfAccountsTemplatesAsync(CancellationToken cancellationToken)
    {
        if (await _db.ChartOfAccountsTemplates.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("ChartOfAccountsTemplate seed: tabloda kayıt mevcut, seed atlandı.");
            return;
        }

        var templates = GetChartOfAccountsTemplates().ToList();
        foreach (var t in templates)
        {
            var entry = _db.ChartOfAccountsTemplates.Add(t);
            entry.Property(nameof(ChartOfAccountsTemplate.Id)).CurrentValue = Guid.CreateVersion7();
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ChartOfAccountsTemplate seed: {Count} kayıt eklendi.", templates.Count);
    }

    /// <summary>TDHP standart hesap planı şablonu (~200 kayıt).</summary>
    private static IEnumerable<ChartOfAccountsTemplate> GetChartOfAccountsTemplates()
    {
        // Helper: (code, parent, name, level, class, type, isRequired, isDetail, isMonetary, displayOrder)
        static ChartOfAccountsTemplate T(
            string code, string? parent, string name,
            AccountLevel level, AccountClass cls, AccountType type,
            bool isRequired, bool isDetail, bool isMonetary, int order) =>
            new()
            {
                Code         = code,
                ParentCode   = parent,
                Name         = name,
                Level        = level,
                AccountClass = cls,
                AccountType  = type,
                IsRequired   = isRequired,
                IsDetail     = isDetail,
                IsMonetary   = isMonetary,
                DisplayOrder = order,
            };

        // Kısaltmalar
        var Main = AccountLevel.Main;
        var Sub  = AccountLevel.Sub;
        var Det  = AccountLevel.Detail;
        var Dv  = AccountClass.CurrentAsset;
        var Dav = AccountClass.NonCurrentAsset;
        var Kv  = AccountClass.ShortTermLiability;
        var Uv  = AccountClass.LongTermLiability;
        var Ok  = AccountClass.Equity;
        var Gel = AccountClass.IncomeStatement;
        var Mal = AccountClass.CostAllocation;
        var Naz = AccountClass.OffBalance;
        var Act = AccountType.Active;
        var Pas = AccountType.Passive;
        var AP  = AccountType.ActivePassive;

        return
        [
            // 1. DÖNEN VARLIKLAR
            T("100",         null,    "Kasa",                                    Main, Dv,  Act, true,  false, true,  1000),
            T("100.01",      "100",   "Merkez Kasa",                             Sub,  Dv,  Act, true,  false, true,  1010),
            T("100.01.001",  "100.01","Ana Kasa",                                Det,  Dv,  Act, true,  true,  true,  1011),
            T("101",         null,    "Alınan Çekler",                           Main, Dv,  Act, true,  false, true,  1020),
            T("101.01",      "101",   "Portföydeki Çekler",                      Sub,  Dv,  Act, true,  false, true,  1021),
            T("101.01.001",  "101.01","Portföydeki Çekler",                      Det,  Dv,  Act, true,  true,  true,  1022),
            T("102",         null,    "Bankalar",                                Main, Dv,  Act, true,  false, true,  1030),
            T("103",         null,    "Verilen Çekler ve Ödeme Emirleri (-)",    Main, Dv,  Pas, true,  false, true,  1040),
            T("108",         null,    "Diğer Hazır Değerler",                    Main, Dv,  Act, false, false, true,  1050),
            T("108.01",      "108",   "Diğer Hazır Değerler",                    Sub,  Dv,  Act, false, false, true,  1051),
            T("108.01.001",  "108.01","Diğer Hazır Değerler",                    Det,  Dv,  Act, false, true,  true,  1052),
            T("110",         null,    "Hisse Senetleri",                         Main, Dv,  Act, false, false, true,  1100),
            T("111",         null,    "Özel Kesim Tahvil Senet ve Bonoları",     Main, Dv,  Act, false, false, true,  1110),
            T("112",         null,    "Kamu Kesimi Tahvil Senet ve Bonoları",    Main, Dv,  Act, false, false, true,  1120),
            T("118",         null,    "Diğer Menkul Kıymetler",                  Main, Dv,  Act, false, false, true,  1180),
            T("119",         null,    "Menkul Kıymetler Değer Düşüklüğü Karş(-)",Main, Dv, Pas, false, false, true,  1190),
            T("120",         null,    "Alıcılar",                                Main, Dv,  Act, true,  false, true,  1200),
            T("120.01",      "120",   "Yurtiçi Alıcılar",                       Sub,  Dv,  Act, true,  false, true,  1201),
            T("120.01.001",  "120.01","Yurtiçi Alıcılar",                        Det,  Dv,  Act, true,  true,  true,  1202),
            T("121",         null,    "Alacak Senetleri",                        Main, Dv,  Act, true,  false, true,  1210),
            T("121.01",      "121",   "Alacak Senetleri",                        Sub,  Dv,  Act, true,  false, true,  1211),
            T("121.01.001",  "121.01","Alacak Senetleri",                        Det,  Dv,  Act, true,  true,  true,  1212),
            T("122",         null,    "Alacak Senetleri Reeskontu (-)",          Main, Dv,  Pas, false, false, true,  1220),
            T("126",         null,    "Verilen Depozito ve Teminatlar",          Main, Dv,  Act, false, false, true,  1260),
            T("128",         null,    "Şüpheli Ticari Alacaklar",                Main, Dv,  Act, false, false, true,  1280),
            T("129",         null,    "Şüpheli Ticari Alacaklar Karşılığı (-)",  Main, Dv,  Pas, false, false, true,  1290),
            T("131",         null,    "Ortaklardan Alacaklar",                   Main, Dv,  Act, false, false, true,  1310),
            T("132",         null,    "İştiraklerden Alacaklar",                 Main, Dv,  Act, false, false, true,  1320),
            T("133",         null,    "Bağlı Ortaklıklardan Alacaklar",          Main, Dv,  Act, false, false, true,  1330),
            T("135",         null,    "Personelden Alacaklar",                   Main, Dv,  Act, false, false, true,  1350),
            T("136",         null,    "Diğer Çeşitli Alacaklar",                 Main, Dv,  Act, false, false, true,  1360),
            T("138",         null,    "Şüpheli Diğer Alacaklar",                 Main, Dv,  Act, false, false, true,  1380),
            T("139",         null,    "Şüpheli Diğer Alacaklar Karşılığı (-)",   Main, Dv,  Pas, false, false, true,  1390),
            T("150",         null,    "İlk Madde ve Malzeme",                    Main, Dv,  Act, true,  false, true,  1500),
            T("151",         null,    "Yarı Mamüller - Üretim",                  Main, Dv,  Act, false, false, true,  1510),
            T("152",         null,    "Mamüller",                                Main, Dv,  Act, false, false, true,  1520),
            T("153",         null,    "Ticari Mallar",                           Main, Dv,  Act, false, false, true,  1530),
            T("157",         null,    "Diğer Stoklar",                           Main, Dv,  Act, false, false, true,  1570),
            T("158",         null,    "Stok Değer Düşüklüğü Karşılığı (-)",      Main, Dv,  Pas, false, false, true,  1580),
            T("159",         null,    "Verilen Sipariş Avansları",               Main, Dv,  Act, false, false, true,  1590),
            T("180",         null,    "Gelecek Aylara Ait Giderler",             Main, Dv,  Act, false, false, true,  1800),
            T("181",         null,    "Gelir Tahakkukları",                      Main, Dv,  Act, false, false, true,  1810),
            T("190",         null,    "Devreden KDV",                            Main, Dv,  Act, true,  false, true,  1900),
            T("190.01",      "190",   "Devreden KDV",                            Sub,  Dv,  Act, true,  false, true,  1901),
            T("190.01.001",  "190.01","Devreden KDV",                            Det,  Dv,  Act, true,  true,  true,  1902),
            T("191",         null,    "İndirilecek KDV",                         Main, Dv,  Act, true,  false, true,  1910),
            T("191.01",      "191",   "İndirilecek KDV (%1)",                    Sub,  Dv,  Act, true,  false, true,  1911),
            T("191.01.001",  "191.01","İndirilecek KDV (%1)",                    Det,  Dv,  Act, true,  true,  true,  1912),
            T("191.10",      "191",   "İndirilecek KDV (%10)",                   Sub,  Dv,  Act, true,  false, true,  1913),
            T("191.10.001",  "191.10","İndirilecek KDV (%10)",                   Det,  Dv,  Act, true,  true,  true,  1914),
            T("191.20",      "191",   "İndirilecek KDV (%20)",                   Sub,  Dv,  Act, true,  false, true,  1915),
            T("191.20.001",  "191.20","İndirilecek KDV (%20)",                   Det,  Dv,  Act, true,  true,  true,  1916),
            T("192",         null,    "Diğer KDV",                               Main, Dv,  AP,  true,  false, true,  1920),
            T("193",         null,    "Peşin Ödenen Vergiler ve Fonlar",         Main, Dv,  Act, true,  false, true,  1930),
            T("193.01",      "193",   "Geçici Vergi",                            Sub,  Dv,  Act, true,  false, true,  1931),
            T("193.01.001",  "193.01","Geçici Vergi",                            Det,  Dv,  Act, true,  true,  true,  1932),
            T("195",         null,    "İş Avansları",                            Main, Dv,  Act, false, false, true,  1950),
            T("196",         null,    "Personel Avansları",                      Main, Dv,  Act, false, false, true,  1960),
            T("197",         null,    "Sayım ve Tesellüm Noksanlıkları",         Main, Dv,  Act, false, false, true,  1970),
            T("198",         null,    "Diğer Çeşitli Dönen Varlıklar",          Main, Dv,  Act, false, false, true,  1980),

            // 2. DURAN VARLIKLAR
            T("220",         null,    "Alıcılar (Uzun Vadeli)",                  Main, Dav, Act, false, false, true,  2200),
            T("221",         null,    "Alacak Senetleri (Uzun Vadeli)",          Main, Dav, Act, false, false, true,  2210),
            T("226",         null,    "Verilen Depozito ve Teminatlar (UV)",     Main, Dav, Act, false, false, true,  2260),
            T("231",         null,    "Ortaklardan Alacaklar (UV)",              Main, Dav, Act, false, false, true,  2310),
            T("242",         null,    "İştirakler",                              Main, Dav, Act, false, false, false, 2420),
            T("245",         null,    "Bağlı Ortaklıklar",                       Main, Dav, Act, false, false, false, 2450),
            T("248",         null,    "Diğer Mali Duran Varlıklar",             Main, Dav, Act, false, false, true,  2480),
            T("250",         null,    "Arazi ve Arsalar",                        Main, Dav, Act, false, false, false, 2500),
            T("250.01",      "250",   "Arazi ve Arsalar",                        Sub,  Dav, Act, false, false, false, 2501),
            T("250.01.001",  "250.01","Arazi ve Arsalar",                        Det,  Dav, Act, false, true,  false, 2502),
            T("251",         null,    "Yeraltı ve Yerüstü Düzenleri",            Main, Dav, Act, false, false, false, 2510),
            T("252",         null,    "Binalar",                                 Main, Dav, Act, true,  false, false, 2520),
            T("252.01",      "252",   "Binalar",                                 Sub,  Dav, Act, true,  false, false, 2521),
            T("252.01.001",  "252.01","Binalar",                                 Det,  Dav, Act, true,  true,  false, 2522),
            T("253",         null,    "Tesis Makine ve Cihazlar",                Main, Dav, Act, false, false, false, 2530),
            T("254",         null,    "Taşıtlar",                                Main, Dav, Act, false, false, false, 2540),
            T("255",         null,    "Demirbaşlar ve Döşemeler",                Main, Dav, Act, true,  false, false, 2550),
            T("255.01",      "255",   "Demirbaşlar ve Döşemeler",                Sub,  Dav, Act, true,  false, false, 2551),
            T("255.01.001",  "255.01","Demirbaşlar ve Döşemeler",                Det,  Dav, Act, true,  true,  false, 2552),
            T("256",         null,    "Diğer Maddi Duran Varlıklar",            Main, Dav, Act, false, false, false, 2560),
            T("257",         null,    "Birikmiş Amortismanlar (-)",              Main, Dav, Pas, true,  false, false, 2570),
            T("257.01",      "257",   "Binalar Birikmiş Amortismanı (-)",        Sub,  Dav, Pas, true,  false, false, 2571),
            T("257.01.001",  "257.01","Binalar Birikmiş Amortismanı (-)",        Det,  Dav, Pas, true,  true,  false, 2572),
            T("258",         null,    "Yapılmakta Olan Yatırımlar",              Main, Dav, Act, false, false, false, 2580),
            T("259",         null,    "Verilen Avanslar (MDV)",                  Main, Dav, Act, false, false, true,  2590),
            T("260",         null,    "Haklar",                                  Main, Dav, Act, false, false, false, 2600),
            T("261",         null,    "Şerefiye",                                Main, Dav, Act, false, false, false, 2610),
            T("263",         null,    "Araştırma ve Geliştirme Giderleri",       Main, Dav, Act, false, false, false, 2630),
            T("264",         null,    "Özel Maliyetler",                         Main, Dav, Act, false, false, false, 2640),
            T("268",         null,    "Birikmiş İtfa Payları (-)",               Main, Dav, Pas, false, false, false, 2680),
            T("280",         null,    "Gelecek Yıllara Ait Giderler",            Main, Dav, Act, false, false, true,  2800),
            T("281",         null,    "Gelir Tahakkukları (Uzun Vadeli)",        Main, Dav, Act, false, false, true,  2810),
            T("291",         null,    "Gelecek Yıllarda İndirilecek KDV",        Main, Dav, Act, false, false, true,  2910),
            T("293",         null,    "Peşin Ödenen Vergiler ve Fonlar (UV)",    Main, Dav, Act, false, false, true,  2930),

            // 3. KISA VADELİ YABANCI KAYNAKLAR
            T("300",         null,    "Banka Kredileri",                         Main, Kv,  Pas, true,  false, true,  3000),
            T("300.01",      "300",   "Kısa Vadeli TL Krediler",                 Sub,  Kv,  Pas, true,  false, true,  3001),
            T("300.01.001",  "300.01","Kısa Vadeli TL Krediler",                 Det,  Kv,  Pas, true,  true,  true,  3002),
            T("301",         null,    "Finansal Kiralama İşlemlerinden Borçlar", Main, Kv,  Pas, false, false, true,  3010),
            T("303",         null,    "UV Kredilerin Anapara Taksitleri",        Main, Kv,  Pas, false, false, true,  3030),
            T("309",         null,    "Diğer Mali Borçlar",                      Main, Kv,  Pas, false, false, true,  3090),
            T("320",         null,    "Satıcılar",                               Main, Kv,  Pas, true,  false, true,  3200),
            T("320.01",      "320",   "Yurtiçi Satıcılar",                       Sub,  Kv,  Pas, true,  false, true,  3201),
            T("320.01.001",  "320.01","Yurtiçi Satıcılar",                       Det,  Kv,  Pas, true,  true,  true,  3202),
            T("321",         null,    "Borç Senetleri",                          Main, Kv,  Pas, true,  false, true,  3210),
            T("321.01",      "321",   "Borç Senetleri",                          Sub,  Kv,  Pas, true,  false, true,  3211),
            T("321.01.001",  "321.01","Borç Senetleri",                          Det,  Kv,  Pas, true,  true,  true,  3212),
            T("326",         null,    "Alınan Depozito ve Teminatlar",           Main, Kv,  Pas, false, false, true,  3260),
            T("331",         null,    "Ortaklara Borçlar",                       Main, Kv,  Pas, false, false, true,  3310),
            T("332",         null,    "İştiraklere Borçlar",                     Main, Kv,  Pas, false, false, true,  3320),
            T("333",         null,    "Bağlı Ortaklıklara Borçlar",              Main, Kv,  Pas, false, false, true,  3330),
            T("335",         null,    "Personele Borçlar",                       Main, Kv,  Pas, true,  false, true,  3350),
            T("335.01",      "335",   "Ödenecek Ücretler",                       Sub,  Kv,  Pas, true,  false, true,  3351),
            T("335.01.001",  "335.01","Ödenecek Ücretler",                       Det,  Kv,  Pas, true,  true,  true,  3352),
            T("336",         null,    "Diğer Çeşitli Borçlar",                   Main, Kv,  Pas, false, false, true,  3360),
            T("340",         null,    "Alınan Sipariş Avansları",                Main, Kv,  Pas, false, false, true,  3400),
            T("349",         null,    "Alınan Diğer Avanslar",                   Main, Kv,  Pas, false, false, true,  3490),
            T("360",         null,    "Ödenecek Vergi ve Fonlar",                Main, Kv,  Pas, true,  false, true,  3600),
            T("360.01",      "360",   "Ödenecek Gelir Vergisi Stopajı",          Sub,  Kv,  Pas, true,  false, true,  3601),
            T("360.01.001",  "360.01","Ödenecek Gelir Vergisi Stopajı",          Det,  Kv,  Pas, true,  true,  true,  3602),
            T("360.02",      "360",   "Ödenecek KDV",                            Sub,  Kv,  Pas, true,  false, true,  3603),
            T("360.02.001",  "360.02","Ödenecek KDV",                            Det,  Kv,  Pas, true,  true,  true,  3604),
            T("361",         null,    "Ödenecek Sosyal Güvenlik Kesintileri",    Main, Kv,  Pas, true,  false, true,  3610),
            T("361.01",      "361",   "Ödenecek SGK Primleri (İşçi)",            Sub,  Kv,  Pas, true,  false, true,  3611),
            T("361.01.001",  "361.01","Ödenecek SGK Primleri (İşçi)",            Det,  Kv,  Pas, true,  true,  true,  3612),
            T("361.02",      "361",   "Ödenecek SGK Primleri (İşveren)",         Sub,  Kv,  Pas, true,  false, true,  3613),
            T("361.02.001",  "361.02","Ödenecek SGK Primleri (İşveren)",         Det,  Kv,  Pas, true,  true,  true,  3614),
            T("368",         null,    "Vadesi Geçmiş Ertelenmiş Vergi",          Main, Kv,  Pas, false, false, true,  3680),
            T("369",         null,    "Ödenecek Diğer Yükümlülükler",            Main, Kv,  Pas, false, false, true,  3690),
            T("370",         null,    "Dönem Kârı Vergi ve Yasal Yük. Karşılığı",Main, Kv,  Pas, false, false, true,  3700),
            T("371",         null,    "Dönem Kârının Peşin Ödenen Vergi(-)",     Main, Kv,  Act, false, false, true,  3710),
            T("372",         null,    "Kıdem Tazminatı Karşılığı",               Main, Kv,  Pas, false, false, true,  3720),
            T("379",         null,    "Diğer Borç ve Gider Karşılıkları",        Main, Kv,  Pas, false, false, true,  3790),
            T("380",         null,    "Gelecek Aylara Ait Gelirler",             Main, Kv,  Pas, false, false, true,  3800),
            T("381",         null,    "Gider Tahakkukları",                      Main, Kv,  Pas, false, false, true,  3810),
            T("391",         null,    "Hesaplanan KDV",                          Main, Kv,  Pas, true,  false, true,  3910),
            T("391.01",      "391",   "Hesaplanan KDV (%1)",                     Sub,  Kv,  Pas, true,  false, true,  3911),
            T("391.01.001",  "391.01","Hesaplanan KDV (%1)",                     Det,  Kv,  Pas, true,  true,  true,  3912),
            T("391.10",      "391",   "Hesaplanan KDV (%10)",                    Sub,  Kv,  Pas, true,  false, true,  3913),
            T("391.10.001",  "391.10","Hesaplanan KDV (%10)",                    Det,  Kv,  Pas, true,  true,  true,  3914),
            T("391.20",      "391",   "Hesaplanan KDV (%20)",                    Sub,  Kv,  Pas, true,  false, true,  3915),
            T("391.20.001",  "391.20","Hesaplanan KDV (%20)",                    Det,  Kv,  Pas, true,  true,  true,  3916),
            T("392",         null,    "Diğer KDV",                               Main, Kv,  AP,  true,  false, true,  3920),
            T("393",         null,    "Merkez ve Şubeler Cari Hesabı",           Main, Kv,  AP,  false, false, true,  3930),
            T("397",         null,    "Sayım ve Tesellüm Fazlaları",             Main, Kv,  Pas, false, false, true,  3970),

            // 4. UZUN VADELİ YABANCI KAYNAKLAR
            T("400",         null,    "Banka Kredileri (UV)",                    Main, Uv,  Pas, false, false, true,  4000),
            T("405",         null,    "Çıkarılmış Tahviller",                    Main, Uv,  Pas, false, false, true,  4050),
            T("420",         null,    "Satıcılar (UV)",                          Main, Uv,  Pas, false, false, true,  4200),
            T("421",         null,    "Borç Senetleri (UV)",                     Main, Uv,  Pas, false, false, true,  4210),
            T("426",         null,    "Alınan Depozito ve Teminatlar (UV)",      Main, Uv,  Pas, false, false, true,  4260),
            T("431",         null,    "Ortaklara Borçlar (UV)",                  Main, Uv,  Pas, false, false, true,  4310),
            T("472",         null,    "Kıdem Tazminatı Karşılığı (UV)",          Main, Uv,  Pas, false, false, true,  4720),
            T("479",         null,    "Diğer Borç ve Gider Karşılıkları (UV)",   Main, Uv,  Pas, false, false, true,  4790),
            T("480",         null,    "Gelecek Yıllara Ait Gelirler (UV)",       Main, Uv,  Pas, false, false, true,  4800),
            T("481",         null,    "Gider Tahakkukları (UV)",                 Main, Uv,  Pas, false, false, true,  4810),
            T("492",         null,    "Gelecek Yıllara Ertelenmiş KDV",          Main, Uv,  Pas, false, false, true,  4920),

            // 5. ÖZKAYNAKLAR
            T("500",         null,    "Sermaye",                                 Main, Ok,  Pas, true,  false, false, 5000),
            T("500.01",      "500",   "Ödenmiş Sermaye",                         Sub,  Ok,  Pas, true,  false, false, 5001),
            T("500.01.001",  "500.01","Ödenmiş Sermaye",                         Det,  Ok,  Pas, true,  true,  false, 5002),
            T("501",         null,    "Ödenmemiş Sermaye (-)",                   Main, Ok,  Act, false, false, false, 5010),
            T("502",         null,    "Sermaye Düzeltmesi Olumlu Farkları",      Main, Ok,  Pas, false, false, false, 5020),
            T("503",         null,    "Sermaye Düzeltmesi Olumsuz Farkları (-)", Main, Ok,  Act, false, false, false, 5030),
            T("510",         null,    "Hisse Senedi İhraç Primleri",             Main, Ok,  Pas, false, false, false, 5100),
            T("520",         null,    "Yasal Yedekler",                          Main, Ok,  Pas, false, false, false, 5200),
            T("521",         null,    "Statü Yedekleri",                         Main, Ok,  Pas, false, false, false, 5210),
            T("522",         null,    "Olağanüstü Yedekler",                     Main, Ok,  Pas, false, false, false, 5220),
            T("529",         null,    "Özel Fonlar",                             Main, Ok,  Pas, false, false, false, 5290),
            T("570",         null,    "Geçmiş Yıllar Kârları",                   Main, Ok,  Pas, true,  false, false, 5700),
            T("570.01",      "570",   "Geçmiş Yıllar Kârları",                   Sub,  Ok,  Pas, true,  false, false, 5701),
            T("570.01.001",  "570.01","Geçmiş Yıllar Kârları",                   Det,  Ok,  Pas, true,  true,  false, 5702),
            T("580",         null,    "Geçmiş Yıllar Zararları (-)",             Main, Ok,  Act, false, false, false, 5800),
            T("590",         null,    "Dönem Net Kârı",                          Main, Ok,  Pas, true,  false, false, 5900),
            T("590.01",      "590",   "Dönem Net Kârı",                          Sub,  Ok,  Pas, true,  false, false, 5901),
            T("590.01.001",  "590.01","Dönem Net Kârı",                          Det,  Ok,  Pas, true,  true,  false, 5902),
            T("591",         null,    "Dönem Net Zararı (-)",                    Main, Ok,  Act, true,  false, false, 5910),
            T("591.01",      "591",   "Dönem Net Zararı (-)",                    Sub,  Ok,  Act, true,  false, false, 5911),
            T("591.01.001",  "591.01","Dönem Net Zararı (-)",                    Det,  Ok,  Act, true,  true,  false, 5912),

            // 6. GELİR TABLOSU HESAPLARI
            T("600",         null,    "Yurtiçi Satışlar",                        Main, Gel, Pas, true,  false, false, 6000),
            T("600.01",      "600",   "Yurtiçi Satışlar",                        Sub,  Gel, Pas, true,  false, false, 6001),
            T("600.01.001",  "600.01","Yurtiçi Satışlar",                        Det,  Gel, Pas, true,  true,  false, 6002),
            T("601",         null,    "Yurtdışı Satışlar",                       Main, Gel, Pas, false, false, false, 6010),
            T("602",         null,    "Diğer Gelirler",                          Main, Gel, Pas, false, false, false, 6020),
            T("610",         null,    "Satıştan İadeler (-)",                    Main, Gel, Act, true,  false, false, 6100),
            T("611",         null,    "Satış İskontoları (-)",                   Main, Gel, Act, false, false, false, 6110),
            T("612",         null,    "Diğer İndirimler (-)",                    Main, Gel, Act, false, false, false, 6120),
            T("620",         null,    "Satılan Mamüller Maliyeti (-)",           Main, Gel, Act, false, false, false, 6200),
            T("621",         null,    "Satılan Ticari Malların Maliyeti (-)",    Main, Gel, Act, true,  false, false, 6210),
            T("621.01",      "621",   "Satılan Ticari Malların Maliyeti (-)",    Sub,  Gel, Act, true,  false, false, 6211),
            T("621.01.001",  "621.01","Satılan Ticari Malların Maliyeti (-)",    Det,  Gel, Act, true,  true,  false, 6212),
            T("622",         null,    "Satılan Hizmet Maliyeti (-)",             Main, Gel, Act, false, false, false, 6220),
            T("630",         null,    "Araştırma ve Geliştirme Giderleri (-)",   Main, Gel, Act, false, false, false, 6300),
            T("631",         null,    "Pazarlama Satış ve Dağıtım Giderleri(-)", Main, Gel, Act, false, false, false, 6310),
            T("632",         null,    "Genel Yönetim Giderleri (-)",             Main, Gel, Act, true,  false, false, 6320),
            T("632.01",      "632",   "Genel Yönetim Giderleri (-)",             Sub,  Gel, Act, true,  false, false, 6321),
            T("632.01.001",  "632.01","Genel Yönetim Giderleri (-)",             Det,  Gel, Act, true,  true,  false, 6322),
            T("640",         null,    "İştiraklerden Temettü Gelirleri",         Main, Gel, Pas, false, false, false, 6400),
            T("641",         null,    "Bağlı Ortaklıklardan Temettü Gelirleri", Main, Gel, Pas, false, false, false, 6410),
            T("642",         null,    "Faiz Gelirleri",                          Main, Gel, Pas, false, false, true,  6420),
            T("643",         null,    "Komisyon Gelirleri",                      Main, Gel, Pas, false, false, false, 6430),
            T("644",         null,    "Konusu Kalmayan Karşılıklar",             Main, Gel, Pas, false, false, false, 6440),
            T("646",         null,    "Kambiyo Kârları",                         Main, Gel, Pas, true,  false, true,  6460),
            T("646.01",      "646",   "Kambiyo Kârları",                         Sub,  Gel, Pas, true,  false, true,  6461),
            T("646.01.001",  "646.01","Kambiyo Kârları",                         Det,  Gel, Pas, true,  true,  true,  6462),
            T("647",         null,    "Reeskont Faiz Gelirleri",                 Main, Gel, Pas, false, false, true,  6470),
            T("648",         null,    "Enflasyon Düzeltmesi Kârları",            Main, Gel, Pas, false, false, true,  6480),
            T("649",         null,    "Diğer Olağan Gelir ve Kârlar",           Main, Gel, Pas, false, false, false, 6490),
            T("653",         null,    "Komisyon Giderleri (-)",                  Main, Gel, Act, false, false, false, 6530),
            T("654",         null,    "Karşılık Giderleri (-)",                  Main, Gel, Act, false, false, false, 6540),
            T("655",         null,    "Menkul Kıymet Satış Zararları (-)",       Main, Gel, Act, false, false, false, 6550),
            T("656",         null,    "Kambiyo Zararları (-)",                   Main, Gel, Act, true,  false, true,  6560),
            T("656.01",      "656",   "Kambiyo Zararları (-)",                   Sub,  Gel, Act, true,  false, true,  6561),
            T("656.01.001",  "656.01","Kambiyo Zararları (-)",                   Det,  Gel, Act, true,  true,  true,  6562),
            T("657",         null,    "Reeskont Faiz Giderleri (-)",             Main, Gel, Act, false, false, true,  6570),
            T("658",         null,    "Enflasyon Düzeltmesi Zararları (-)",      Main, Gel, Act, false, false, true,  6580),
            T("659",         null,    "Diğer Olağan Gider ve Zararlar (-)",     Main, Gel, Act, false, false, false, 6590),
            T("660",         null,    "Kısa Vadeli Borçlanma Giderleri (-)",    Main, Gel, Act, true,  false, true,  6600),
            T("660.01",      "660",   "Kısa Vadeli Banka Faizi (-)",             Sub,  Gel, Act, true,  false, true,  6601),
            T("660.01.001",  "660.01","Kısa Vadeli Banka Faizi (-)",             Det,  Gel, Act, true,  true,  true,  6602),
            T("661",         null,    "Uzun Vadeli Borçlanma Giderleri (-)",    Main, Gel, Act, false, false, true,  6610),
            T("671",         null,    "Önceki Dönem Gelir ve Kârlar",           Main, Gel, Pas, false, false, false, 6710),
            T("679",         null,    "Diğer Olağandışı Gelir ve Kârlar",       Main, Gel, Pas, false, false, false, 6790),
            T("680",         null,    "Çalışmayan Kısım Gider ve Zararları(-)", Main, Gel, Act, false, false, false, 6800),
            T("681",         null,    "Önceki Dönem Gider ve Zararları (-)",    Main, Gel, Act, false, false, false, 6810),
            T("689",         null,    "Diğer Olağandışı Gider ve Zararlar(-)", Main, Gel, Act, false, false, false, 6890),
            T("690",         null,    "Dönem Kârı veya Zararı",                 Main, Gel, AP,  true,  false, false, 6900),
            T("690.01",      "690",   "Dönem Kârı veya Zararı",                 Sub,  Gel, AP,  true,  false, false, 6901),
            T("690.01.001",  "690.01","Dönem Kârı veya Zararı",                 Det,  Gel, AP,  true,  true,  false, 6902),
            T("691",         null,    "Dönem Kârı Vergi ve Yasal Yük. Karş(-)", Main, Gel, Act, false, false, false, 6910),
            T("692",         null,    "Dönem Net Kârı veya Zararı",             Main, Gel, AP,  false, false, false, 6920),
            T("698",         null,    "Enflasyon Düzeltmesi Hesabı",            Main, Gel, AP,  true,  false, true,  6980),
            T("698.01",      "698",   "Enflasyon Düzeltmesi Hesabı",            Sub,  Gel, AP,  true,  false, true,  6981),
            T("698.01.001",  "698.01","Enflasyon Düzeltmesi Hesabı",            Det,  Gel, AP,  true,  true,  true,  6982),

            // 7. MALİYET / YANSITMA HESAPLARI
            T("710",         null,    "Direkt İlk Madde ve Malzeme Giderleri",  Main, Mal, Act, false, false, false, 7100),
            T("711",         null,    "Direkt İlk Madde Giderleri Yansıtma",    Main, Mal, Pas, false, false, false, 7110),
            T("720",         null,    "Direkt İşçilik Giderleri",               Main, Mal, Act, false, false, false, 7200),
            T("720.01",      "720",   "Brüt Ücretler",                          Sub,  Mal, Act, false, false, false, 7201),
            T("720.01.001",  "720.01","Brüt Ücretler",                          Det,  Mal, Act, false, true,  false, 7202),
            T("721",         null,    "Direkt İşçilik Giderleri Yansıtma",      Main, Mal, Pas, false, false, false, 7210),
            T("730",         null,    "Genel Üretim Giderleri",                 Main, Mal, Act, false, false, false, 7300),
            T("731",         null,    "Genel Üretim Giderleri Yansıtma",        Main, Mal, Pas, false, false, false, 7310),
            T("740",         null,    "Hizmet Üretim Maliyeti",                 Main, Mal, Act, false, false, false, 7400),
            T("741",         null,    "Hizmet Üretim Maliyeti Yansıtma",        Main, Mal, Pas, false, false, false, 7410),
            T("750",         null,    "Araştırma ve Geliştirme Giderleri",      Main, Mal, Act, false, false, false, 7500),
            T("751",         null,    "Araştırma ve Geliştirme Gid. Yansıtma", Main, Mal, Pas, false, false, false, 7510),
            T("760",         null,    "Pazarlama Satış ve Dağıtım Giderleri",   Main, Mal, Act, false, false, false, 7600),
            T("761",         null,    "Pazarlama Satış Dağıtım Gid. Yansıtma", Main, Mal, Pas, false, false, false, 7610),
            T("770",         null,    "Genel Yönetim Giderleri",                Main, Mal, Act, true,  false, false, 7700),
            T("770.01",      "770",   "Personel Giderleri",                     Sub,  Mal, Act, true,  false, false, 7701),
            T("770.01.001",  "770.01","Personel Giderleri",                     Det,  Mal, Act, true,  true,  false, 7702),
            T("770.02",      "770",   "Kira Giderleri",                         Sub,  Mal, Act, true,  false, false, 7703),
            T("770.02.001",  "770.02","Kira Giderleri",                         Det,  Mal, Act, true,  true,  false, 7704),
            T("770.03",      "770",   "Elektrik Su Gaz Giderleri",              Sub,  Mal, Act, true,  false, false, 7705),
            T("770.03.001",  "770.03","Elektrik Su Gaz Giderleri",              Det,  Mal, Act, true,  true,  false, 7706),
            T("771",         null,    "Genel Yönetim Giderleri Yansıtma",       Main, Mal, Pas, true,  false, false, 7710),
            T("771.01",      "771",   "Genel Yönetim Giderleri Yansıtma",       Sub,  Mal, Pas, true,  false, false, 7711),
            T("771.01.001",  "771.01","Genel Yönetim Giderleri Yansıtma",       Det,  Mal, Pas, true,  true,  false, 7712),
            T("780",         null,    "Finansman Giderleri",                    Main, Mal, Act, true,  false, true,  7800),
            T("780.01",      "780",   "Kısa Vadeli Finansman Giderleri",        Sub,  Mal, Act, true,  false, true,  7801),
            T("780.01.001",  "780.01","Kısa Vadeli Finansman Giderleri",        Det,  Mal, Act, true,  true,  true,  7802),
            T("781",         null,    "Finansman Giderleri Yansıtma",           Main, Mal, Pas, false, false, true,  7810),

            // 9. NAZIM HESAPLAR
            T("900",         null,    "Teminat Mektupları",                      Main, Naz, Act, false, false, false, 9000),
            T("900.01",      "900",   "Alınan Teminat Mektupları",              Sub,  Naz, Act, false, false, false, 9001),
            T("900.01.001",  "900.01","Alınan Teminat Mektupları",              Det,  Naz, Act, false, true,  false, 9002),
            T("901",         null,    "Teminat Mektupları Karşılığı",           Main, Naz, Pas, false, false, false, 9010),
            T("901.01",      "901",   "Alınan Teminat Mektupları Karşılığı",    Sub,  Naz, Pas, false, false, false, 9011),
            T("901.01.001",  "901.01","Alınan Teminat Mektupları Karşılığı",    Det,  Naz, Pas, false, true,  false, 9012),
            T("906",         null,    "Aktife Alınmayan Kiralık Kıymetler",     Main, Naz, Act, false, false, false, 9060),
            T("907",         null,    "Aktife Alınmayan Kir. Kıym. Karşılığı", Main, Naz, Pas, false, false, false, 9070),
            T("910",         null,    "Çıkarılmış Sigorta Poliçeleri",          Main, Naz, Act, false, false, false, 9100),
            T("911",         null,    "Çıkarılmış Sigorta Poliçeleri Karşılığı",Main, Naz, Pas, false, false, false, 9110),
            T("950",         null,    "İşletme Adına Yapılan Tahsilatlar",      Main, Naz, Act, false, false, true,  9500),
            T("951",         null,    "İşletme Adına Yapılan Ödemeler",         Main, Naz, Pas, false, false, true,  9510),
            T("960",         null,    "Emanet Senetler",                        Main, Naz, Act, false, false, false, 9600),
            T("961",         null,    "Emanet Senetler Karşılığı",              Main, Naz, Pas, false, false, false, 9610),
            T("980",         null,    "Çal. Sağlanan Faydalara İlişkin Borçlar",Main, Naz, Act, false, false, false, 9800),
            T("981",         null,    "Çal. Sağlanan Fay. İlişkin Borç. Karş.",Main, Naz, Pas, false, false, false, 9810),
        ];
    }

    /// <summary>
    /// Bütçe tipi sistem kataloğunu idempotent olarak seed eder. Her tip için
    /// base 120/600 hesap kodları tanımlanır; ilk tahakkukta bunların altına
    /// şirkete özel alt hesap üretilir (örn. Aidat → 120.01 → 120.01.001).
    /// </summary>
    private async Task SeedBudgetTypeMetadataAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.BudgetTypeMetadata
            .ToDictionaryAsync(m => m.Type, m => m, cancellationToken);

        var added = 0;
        var updated = 0;
        foreach (var def in GetBudgetTypeMetadata())
        {
            if (existing.TryGetValue(def.Type, out var current))
            {
                var dirty = false;
                if (current.DisplayName != def.DisplayName) { current.DisplayName = def.DisplayName; dirty = true; }
                if (current.BaseReceivableCode != def.BaseReceivableCode) { current.BaseReceivableCode = def.BaseReceivableCode; dirty = true; }
                if (current.BaseIncomeCode != def.BaseIncomeCode) { current.BaseIncomeCode = def.BaseIncomeCode; dirty = true; }
                if (current.DefaultPaymentSchedule != def.DefaultPaymentSchedule) { current.DefaultPaymentSchedule = def.DefaultPaymentSchedule; dirty = true; }
                if (current.AllowMultiplePerYear != def.AllowMultiplePerYear) { current.AllowMultiplePerYear = def.AllowMultiplePerYear; dirty = true; }
                if (current.DisplayOrder != def.DisplayOrder) { current.DisplayOrder = def.DisplayOrder; dirty = true; }
                if (dirty) updated++;
                continue;
            }

            var entry = _db.BudgetTypeMetadata.Add(def);
            entry.Property(nameof(BudgetTypeMetadata.Id)).CurrentValue = Guid.CreateVersion7();
            added++;
        }

        if (added > 0 || updated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "BudgetTypeMetadata seed: {Added} eklendi, {Updated} güncellendi.", added, updated);
    }

    /// <summary>Varsayılan bütçe tipleri — base hesap kodları (120.0X / 600.0X).</summary>
    private static IEnumerable<BudgetTypeMetadata> GetBudgetTypeMetadata() =>
    [
        new BudgetTypeMetadata
        {
            Type = BudgetType.Aidat,
            DisplayName = "Aidat Bütçesi",
            BaseReceivableCode = "120.01",
            BaseIncomeCode = "600.01",
            DefaultPaymentSchedule = PaymentSchedule.MonthlyEqual,
            AllowMultiplePerYear = true,
            DisplayOrder = 1,
            IsActive = true,
        },
        new BudgetTypeMetadata
        {
            Type = BudgetType.Yatirim,
            DisplayName = "Yatırım Bütçesi",
            BaseReceivableCode = "120.02",
            BaseIncomeCode = "600.02",
            DefaultPaymentSchedule = PaymentSchedule.Installment,
            AllowMultiplePerYear = true,
            DisplayOrder = 2,
            IsActive = true,
        },
        new BudgetTypeMetadata
        {
            Type = BudgetType.Komur,
            DisplayName = "Kömür/Yakıt Bütçesi",
            BaseReceivableCode = "120.03",
            BaseIncomeCode = "600.03",
            DefaultPaymentSchedule = PaymentSchedule.Installment,
            AllowMultiplePerYear = true,
            DisplayOrder = 3,
            IsActive = true,
        },
        new BudgetTypeMetadata
        {
            Type = BudgetType.Kurulus,
            DisplayName = "Kuruluş Bütçesi",
            BaseReceivableCode = "120.04",
            BaseIncomeCode = "600.04",
            DefaultPaymentSchedule = PaymentSchedule.Installment,
            AllowMultiplePerYear = true,
            DisplayOrder = 4,
            IsActive = true,
        },
    ];
}
