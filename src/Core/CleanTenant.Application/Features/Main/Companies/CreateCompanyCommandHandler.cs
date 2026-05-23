using System.Security.Cryptography;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Notifications;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Readers;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using CleanTenant.SharedKernel.Context;
using CleanTenant.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <para>
/// <see cref="CreateCompanyCommand"/> handler. Site (Main DB) yaratır ve aynı akışta
/// zorunlu bir <b>CompanyAdmin</b> (süper company kullanıcısı) provision eder
/// (v0.2.13.e). Akış <see cref="Catalog.Tenants.CreateTenantCommandHandler"/>'ın
/// Company karşılığıdır.
/// </para>
/// <para>
/// <b>Çapraz-DB:</b> <see cref="Company"/> Main DB'de, kullanıcı/rol/atama Catalog
/// DB'de yaşar. Bu yüzden iki ayrı SaveChanges gerekir; dağıtık transaction yoktur
/// (tenant akışıyla aynı profil). Provisioning adımları best-effort sıralanır.
/// </para>
/// </summary>
public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<CompanyDetail>>
{
    private readonly IMainDbContext _mainDb;
    private readonly ICatalogDbContext _catalog;
    private readonly UserManager<User> _userManager;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly IEmailSender _email;
    private readonly IClock _clock;
    private readonly ILogger<CreateCompanyCommandHandler> _logger;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateCompanyCommandHandler(
        IMainDbContext mainDb,
        ICatalogDbContext catalog,
        UserManager<User> userManager,
        ICacheInvalidator cacheInvalidator,
        IEmailSender email,
        IClock clock,
        ILogger<CreateCompanyCommandHandler> logger)
    {
        _mainDb = mainDb;
        _catalog = catalog;
        _userManager = userManager;
        _cacheInvalidator = cacheInvalidator;
        _email = email;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CompanyDetail>> Handle(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        // 1. CompanyAdmin rolünü doğrula (seed'den gelir, ScopeLevel=Company) — fail-fast.
        var companyAdminRole = await _catalog.Roles.AsNoTracking()
            .Where(r => r.Scope == ScopeLevel.Company && r.NormalizedName == "COMPANYADMIN")
            .Select(r => new { r.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (companyAdminRole is null)
        {
            return Result<CompanyDetail>.Failure(
                Error.Critical("ROLE-COMPANY-ADMIN-NOT-SEEDED",
                    "CompanyAdmin rolü seed edilmemiş. Seed işlemini çalıştırın."));
        }

        var now = _clock.UtcNow;

        // 2. Site (Company) — Main DB. Id + UrlCode SaveChanges sonrası dolar.
        var company = new Company
        {
            TenantId = command.TenantId,
            Name = command.Name,
            LegalName = command.LegalName,
            Vkn = command.Vkn,
            Email = command.Email,
            Phone = command.Phone,
            Status = CompanyStatus.Active,
        };
        _mainDb.Companies.Add(company);
        await _mainDb.SaveChangesAsync(cancellationToken);

        // 3. Site yöneticisi User — mevcut e-posta varsa reuse (bir kişi birden çok
        //    sitede CompanyAdmin olabilir), yoksa temp password ile oluştur.
        var existingEmailUser = await _userManager.FindByEmailAsync(command.AdminEmail);
        User user;
        bool isNewUser;
        if (existingEmailUser is not null)
        {
            user = existingEmailUser;
            isNewUser = false;
        }
        else
        {
            user = new User
            {
                UserName = command.AdminEmail,
                Email = command.AdminEmail,
                EmailConfirmed = false,
                FirstName = command.AdminFirstName,
                LastName = command.AdminLastName,
                PhoneNumber = command.AdminPhone,
                PhoneNumberConfirmed = false,
            };

            var tempPassword = GenerateTempPassword();
            var createResult = await _userManager.CreateAsync(user, tempPassword);
            if (!createResult.Succeeded)
            {
                return Result<CompanyDetail>.Failure(
                    Error.Validation("USER-CREATE-FAILED",
                        "Site yöneticisi oluşturulamadı: " +
                        string.Join("; ", createResult.Errors.Select(e => e.Description))));
            }
            isNewUser = true;
        }

        // 4. UserRoleAssignment — CompanyAdmin / Company scope.
        _catalog.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = companyAdminRole.Id,
            ScopeLevel = ScopeLevel.Company,
            TenantId = command.TenantId,
            CompanyId = company.Id,
            UnitId = null,
            AssignedAt = now,
            IsActive = true,
        });
        await _catalog.SaveChangesAsync(cancellationToken);

        // 5. E-posta bildirimi (best-effort).
        try
        {
            if (isNewUser)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                await SendWelcomeEmailAsync(user, company, resetToken, cancellationToken);
            }
            else
            {
                await SendAddedToCompanyEmailAsync(user, company, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Site yöneticisi bildirim e-postası gönderilemedi (CompanyId={CompanyId}, UserId={UserId}). Site oluşturuldu.",
                company.Id, user.Id);
        }

        // 6. Cache invalidate — yeni site Context Switcher'da görünmeli.
        await _cacheInvalidator.InvalidateCompanyAsync(company.Id, company.TenantId, cancellationToken);
        await _cacheInvalidator.InvalidateAllUserContextsAsync(cancellationToken);

        _logger.LogInformation(
            "Yeni Site oluşturuldu: {CompanyName} (Id={CompanyId}, UrlCode={UrlCode}) — Admin {AdminEmail}",
            company.Name, company.Id, company.UrlCode, user.Email);

        return Result<CompanyDetail>.Success(new CompanyDetail(
            company.Id,
            company.TenantId,
            company.UrlCode,
            company.Name,
            company.LegalName,
            company.Vkn,
            company.Email,
            company.Phone,
            company.Status));
    }

    /// <summary>
    /// Mevcut bir kullanıcı yeni bir Site'a CompanyAdmin atandığında bilgilendirme
    /// e-postası (şifre/hesap aksiyonu yok — sadece bağlam bildirimi).
    /// </summary>
    private async Task SendAddedToCompanyEmailAsync(User user, Company company, CancellationToken ct)
    {
        var subject = $"CleanTenant — {company.Name} Sitesi'ne yönetici olarak atandınız";
        var body =
            $"""
            Sayın {user.FirstName} {user.LastName},

            Mevcut hesabınızla '{company.Name}' Sitesi'nde Site Yöneticisi (CompanyAdmin)
            olarak atandınız. Bu siteye erişmek için ManagementApp'e mevcut şifrenizle
            giriş yapabilir, AppBar'daki bağlam (context) seçicisinden ilgili siteye
            geçebilirsiniz.

            E-posta: {user.Email}
            Site: {company.Name}
            Site Kodu: {company.UrlCode}

            Bu işlemi siz talep etmediyseniz lütfen Yönetim yöneticinize bildirin.

            CleanTenant — Toplu Yapı Yönetimi
            """;

        await _email.SendAsync(user.Email!, subject, body, ct);
    }

    private async Task SendWelcomeEmailAsync(User user, Company company, string resetToken, CancellationToken ct)
    {
        var encodedToken = Uri.EscapeDataString(resetToken);
        var encodedEmail = Uri.EscapeDataString(user.Email!);
        // NOTE: ManagementApp base URL config'ten gelmeli (Faz 1.5+); şimdi placeholder.
        var resetLink = $"{{ManagementAppBaseUrl}}/auth/reset-password?email={encodedEmail}&token={encodedToken}";

        var subject = $"CleanTenant — {company.Name} Site Yöneticisi Hesabınız";
        var body =
            $"""
            Sayın {user.FirstName} {user.LastName},

            '{company.Name}' Sitesi'nde Site Yöneticisi (CompanyAdmin) olarak atandınız.
            Hesabınızı aktifleştirmek ve şifrenizi belirlemek için aşağıdaki bağlantıyı
            kullanın (24 saat geçerli):

            {resetLink}

            E-posta: {user.Email}
            Site: {company.Name}
            Site Kodu: {company.UrlCode}

            Bu bağlantı yalnızca sizin içindir. Eğer bu mesajı yanlışlıkla aldıysanız
            lütfen Yönetim yöneticinize bildirin.

            CleanTenant — Toplu Yapı Yönetimi
            """;

        await _email.SendAsync(user.Email!, subject, body, ct);
    }

    /// <summary>
    /// Geçici şifre — Identity password policy'sine uyacak şekilde 12 karakter
    /// (büyük + küçük + rakam + özel). Reset token akışı bu şifreyi geçersiz kılar.
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

        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }
}
