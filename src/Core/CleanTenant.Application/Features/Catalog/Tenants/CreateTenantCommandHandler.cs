using System.Security.Cryptography;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenantEntity = CleanTenant.Domain.Identity.Tenants.Tenant;
using TenantStatus = CleanTenant.Domain.Identity.Tenants.TenantStatus;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <para>
/// <see cref="CreateTenantCommand"/> handler. İş akışı:
/// </para>
/// <list type="number">
///   <item>Çakışma kontrolü: Yönetim adı ve kimlik numarası tekil mi.</item>
///   <item>Catalog DB transaction başlat.</item>
///   <item><see cref="Tenant"/> entity yarat ve persist et.</item>
///   <item><see cref="User"/> entity (Sorumlu Yönetici) yarat — UserManager ile.</item>
///   <item><c>TenantAdmin</c> rolünü bul → <see cref="UserRoleAssignment"/> yarat (Tenant scope).</item>
///   <item>Transaction commit.</item>
///   <item>Password reset token üret → <see cref="IEmailSender"/> ile Welcome email gönder.</item>
///   <item>Cache invalidate: tüm tenant listesi yenilenir.</item>
/// </list>
/// </summary>
public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<CreateTenantResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IEmailSender _email;
    private readonly IClock _clock;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateTenantCommandHandler(
        UserManager<User> userManager,
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        IEmailSender email,
        IClock clock,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _userManager = userManager;
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _email = email;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CreateTenantResult>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        // 1. Tekillik kontrolleri (DB'deki unique index yine son savunma)
        var nameExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Name == command.Name && !t.IsDeleted, cancellationToken);
        if (nameExists)
        {
            return Result<CreateTenantResult>.Failure(
                Error.Conflict("TENANT-NAME-EXISTS", $"'{command.Name}' adında bir Yönetim zaten kayıtlı."));
        }

        var identityExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.LegalIdentityNumber == command.LegalIdentityNumber && !t.IsDeleted, cancellationToken);
        if (identityExists)
        {
            return Result<CreateTenantResult>.Failure(
                Error.Conflict("TENANT-LEGAL-ID-EXISTS",
                    $"Bu kimlik numarasıyla ({command.LegalIdentityNumber}) kayıtlı bir Yönetim mevcut."));
        }

        var existingEmailUser = await _userManager.FindByEmailAsync(command.AdminEmail);
        if (existingEmailUser is not null)
        {
            return Result<CreateTenantResult>.Failure(
                Error.Conflict("ADMIN-EMAIL-EXISTS",
                    $"'{command.AdminEmail}' e-postasıyla kayıtlı bir kullanıcı zaten var."));
        }

        // 2. TenantAdmin rolünü bul (seed'den gelir, ScopeLevel=Tenant)
        var tenantAdminRole = await _db.Roles.AsNoTracking()
            .Where(r => r.Scope == ScopeLevel.Tenant && r.NormalizedName == "TENANTADMIN")
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenantAdminRole is null)
        {
            return Result<CreateTenantResult>.Failure(
                Error.Critical("ROLE-TENANT-ADMIN-NOT-SEEDED",
                    "TenantAdmin rolü seed edilmemiş. Seed işlemini çalıştırın."));
        }

        var now = _clock.UtcNow;

        // 3. Tenant entity
        var tenant = new TenantEntity
        {
            Name = command.Name,
            LegalName = command.LegalName,
            LegalIdentityType = command.LegalIdentityType,
            LegalIdentityNumber = command.LegalIdentityNumber,
            Address = command.Address,
            Status = TenantStatus.Active,
            BillingTier = command.BillingTier,
            HasDedicatedDatabase = command.HasDedicatedDatabase,
            AllowSystemWriteAccess = true,
        };
        _db.Tenants.Add(tenant);

        // 4. Sorumlu Yönetici User (UserManager ile)
        var user = new User
        {
            UserName = command.AdminEmail,
            Email = command.AdminEmail,
            EmailConfirmed = false,
            FirstName = command.AdminFirstName,
            LastName = command.AdminLastName,
            PhoneNumber = command.AdminPhone,
            PhoneNumberConfirmed = false,
        };

        // Geçici rastgele şifre — reset token ile değiştirilecek
        var tempPassword = GenerateTempPassword();
        var createResult = await _userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
        {
            return Result<CreateTenantResult>.Failure(
                Error.Validation("USER-CREATE-FAILED",
                    "Sorumlu Yönetici oluşturulamadı: " +
                    string.Join("; ", createResult.Errors.Select(e => e.Description))));
        }

        // 5. UserRoleAssignment — TenantAdmin / Tenant scope
        var assignment = new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = tenantAdminRole.Id,
            ScopeLevel = ScopeLevel.Tenant,
            TenantId = tenant.Id,
            CompanyId = null,
            UnitId = null,
            AssignedAt = now,
            IsActive = true,
        };
        _db.UserRoleAssignments.Add(assignment);

        // 6. SaveChanges (Tenant + Assignment — User UserManager.CreateAsync ile zaten kaydedildi)
        await _db.SaveChangesAsync(cancellationToken);

        // 7. Password reset token + welcome email (best-effort; başarısız olursa
        // tenant yine yaratılır, manuel reset gerekir).
        try
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            await SendWelcomeEmailAsync(user, tenant, resetToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Welcome email gönderilemedi (TenantId={TenantId}, UserId={UserId}). Yönetim oluşturuldu; admin manuel reset yapmalı.",
                tenant.Id, user.Id);
        }

        // 8. Cache invalidate (list cache + global companies)
        await _cacheInvalidator.InvalidateAllTenantsAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni Yönetim oluşturuldu: {TenantName} (Id={TenantId}, UrlCode={UrlCode}) — Admin {AdminEmail}",
            tenant.Name, tenant.Id, tenant.UrlCode, user.Email);

        return Result<CreateTenantResult>.Success(new CreateTenantResult(
            tenant.Id,
            tenant.UrlCode,
            user.Id,
            user.Email!));
    }

    private async Task SendWelcomeEmailAsync(User user, TenantEntity tenant, string resetToken, CancellationToken ct)
    {
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(user.Email!);
        // NOTE: ManagementApp base URL config'ten gelmeli (Faz 1.5+); şimdi
        // placeholder + reset link mail içeriğine eklenir.
        var resetLink = $"{{ManagementAppBaseUrl}}/auth/reset-password?email={encodedEmail}&token={encodedToken}";

        var subject = $"CleanTenant — {tenant.Name} Yönetim Admin Hesabınız";
        var body =
            $"""
            Sayın {user.FirstName} {user.LastName},

            '{tenant.Name}' Yönetim'inde Yönetim Admin (TenantAdmin) olarak atandınız.
            Hesabınızı aktifleştirmek ve şifrenizi belirlemek için aşağıdaki bağlantıyı
            kullanın (24 saat geçerli):

            {resetLink}

            E-posta: {user.Email}
            Yönetim Adı: {tenant.Name}
            Yönetim Kodu: {tenant.UrlCode}

            Bu bağlantı yalnızca sizin içindir. Eğer bu mesajı yanlışlıkla aldıysanız
            lütfen sistem yöneticisine bildirin.

            CleanTenant — Toplu Yapı Yönetimi
            """;

        await _email.SendAsync(user.Email!, subject, body, ct);
    }

    /// <summary>
    /// Geçici şifre — Identity password policy'sine uyacak şekilde 12 char
    /// (büyük + küçük + rakam + özel karakter). Reset token akışı zaten bu
    /// şifreyi geçersiz kılacak.
    /// </summary>
    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digit = "23456789";
        const string special = "!@#$%^&*";

        Span<char> chars = stackalloc char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[2] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[3] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[4] = digit[RandomNumberGenerator.GetInt32(digit.Length)];
        chars[5] = digit[RandomNumberGenerator.GetInt32(digit.Length)];
        chars[6] = special[RandomNumberGenerator.GetInt32(special.Length)];

        var pool = upper + lower + digit + special;
        for (int i = 7; i < chars.Length; i++)
        {
            chars[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }

        // Karıştır
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}
