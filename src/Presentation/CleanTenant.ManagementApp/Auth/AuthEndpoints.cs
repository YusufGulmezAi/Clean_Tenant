using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Features.Auth.Login;
using CleanTenant.Application.Features.Auth.Logout;
using CleanTenant.Application.Features.Auth.PasswordChange;
using CleanTenant.Application.Features.Auth.PasswordReset;
using CleanTenant.Application.Features.Auth.Tenants;
using CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;
using CleanTenant.Application.Features.Auth.TwoFactor.SendCode;
using CleanTenant.Application.Features.Auth.TwoFactor.VerifyTwoFactor;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.ManagementApp.Auth;

/// <summary>
/// Blazor Server için cookie set/sil endpoint'leri. Razor form'ları buraya POST
/// eder — handler IMediator.Send ile backend'i çalıştırır ve HttpContext üzerinden
/// cookie set'ler veya siler.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>SessionLookupMiddleware'in Redis lookup için kullandığı JWT claim adı.</summary>
    public const string SidClaim = "sid";

    /// <summary>v0.2.3.c — UserId claim adı (JWT'nin "sub" claim'i; ClaimTypes.NameIdentifier'a da yazılır).</summary>
    public const string UserIdClaim = "user_id";

    /// <summary>Sekme/persona context kimliği claim adı.</summary>
    public const string ContextIdClaim = "ctx";

    /// <summary>Aktif scope seviyesi claim adı (System/Tenant/Company/Unit).</summary>
    public const string ScopeClaim = "scope";

    /// <summary>v0.2.3.b — Aktif tenant kimliği claim adı (UI gösterimi için, Blazor circuit'inde HttpUserContext null olduğundan claim'den okuyoruz).</summary>
    public const string TenantIdClaim = "tenant_id";

    /// <summary>v0.2.3.b — Aktif tenant adı claim adı (AppBar bağlam etiketi).</summary>
    public const string TenantNameClaim = "tenant_name";

    /// <summary>v0.2.3.b — Aktif şirket (company) kimliği claim adı.</summary>
    public const string CompanyIdClaim = "company_id";

    /// <summary>v0.2.3.b — Aktif şirket adı claim adı (CompanyName AuthSession'da denormalize edildiğinde set edilir).</summary>
    public const string CompanyNameClaim = "company_name";

    // İlk giriş şifre değişimi challenge cookie adı.
    internal const string PasswordChangeCookieName = "__ct_pwd_chg";

    // Şifre sıfırlama akışında e-posta adresini taşır.
    internal const string ForgotPasswordEmailCookieName = "__ct_pwd_rst";

    // v0.2.2.d — 2FA challenge / enrollment token'ları artık query string yerine
    // kısa ömürlü HttpOnly cookie ile taşınır. Önceki davranışta token URL bar'da
    // görünüyordu: hem profesyonel olmayan görüntü, hem browser history / Referer
    // header / server log'larında leak. HttpOnly cookie XSS'e karşı da korur.
    internal const string ChallengeCookieName = "__ct_2fa_chal";
    internal const string EnrollmentCookieName = "__ct_2fa_enroll";

    // Challenge token backend'de 5 dk; cookie TTL'ini eşle.
    private static readonly CookieOptions ChallengeCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(5),
        IsEssential = true,
    };

    // Enrollment challenge backend'de 10 dk (UI'da yazılı).
    private static readonly CookieOptions EnrollmentCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(10),
        IsEssential = true,
    };

    // Şifre değişim challenge 15 dk.
    private static readonly CookieOptions PasswordChangeCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(15),
        IsEssential = true,
    };

    // Şifre sıfırlama OTP 15 dk.
    private static readonly CookieOptions ForgotPasswordCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(15),
        IsEssential = true,
    };

    // Cookie sil: Delete'in attribute'ları set ile birebir uyuşmalı, aksi halde
    // browser farklı cookie sayar ve eskisi kalır.
    private static readonly CookieOptions DeleteCookieOptions = new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };

    /// <summary>Cookie auth endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // v0.2.2 — Login form post handler. ManagementApp Login sayfası bunu form action olarak kullanır.
        routes.MapPost("/auth/sign-in", SignInAsync)
              .DisableAntiforgery() // form post; CSRF için Faz 1.2'de antiforgery token eklenir
              .AllowAnonymous();

        routes.MapPost("/auth/sign-out", SignOutAsync)
              .DisableAntiforgery();

        // v0.2.2 — MudMenuItem href'inden GET kabul edilir; Faz 1.X'te antiforgery POST-only.
        routes.MapGet("/auth/sign-out", SignOutAsync);

        routes.MapPost("/auth/2fa/verify", Verify2FaAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        routes.MapPost("/auth/2fa/send-code", Send2FaCodeAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // v0.2.2.a — Pre-auth 2FA enrollment finalize.
        // Sayfa (TwoFactorEnrollmentPreAuth.razor) InteractiveServer modunda
        // Start + Complete'i IMediator ile in-process çağırır; finalize ise
        // cookie set'lemesi için HttpContext'e ulaşan bir endpoint olmalı —
        // bu yüzden form post pattern'i kullanılır (Login.razor gibi).
        routes.MapPost("/auth/2fa/enroll-pre-auth/finalize", FinalizePreAuthEnrollmentAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // v0.2.3.b — AppBar "Aktif Tenant" dropdown form post.
        // SwitchTenantCommand çalıştırır, dönen TokenPair ile cookie'yi yeniler ve
        // kullanıcıyı belirtilen returnUrl'e (default "/") yönlendirir.
        routes.MapPost("/auth/switch-tenant", SwitchTenantFormAsync)
              .DisableAntiforgery()
              .RequireAuthorization();

        // v0.2.3.b — System scope'a geri dönüş.
        routes.MapPost("/auth/switch-to-system", SwitchToSystemFormAsync)
              .DisableAntiforgery()
              .RequireAuthorization();

        // v0.2.10.d — Dil tercih değiştirme. Cookie set + (giriş yapmışsa)
        // User.PreferredCulture DB'ye yazılır. Anonim kullanıcılar için yalnız
        // cookie set'lenir (login sonrası PreferredCulture'dan üzerine yazılır).
        routes.MapPost("/auth/change-culture", ChangeCultureFormAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // İlk giriş zorunlu şifre değişimi finalize.
        routes.MapPost("/auth/change-password", CompletePasswordChangeAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        // Şifre sıfırlama: OTP gönder + doğrula+sıfırla.
        routes.MapPost("/auth/forgot-password", ForgotPasswordAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        routes.MapPost("/auth/reset-password", ResetPasswordAsync)
              .DisableAntiforgery()
              .AllowAnonymous();

        return routes;
    }

    private static async Task<IResult> Verify2FaAsync(
        HttpContext httpContext,
        [FromForm] string method,
        [FromForm] string code,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadChallengeCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new VerifyTwoFactorCommand(challengeToken, method, code, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            // Cookie hâlâ geçerli — kullanıcı kodu tekrar deneyebilsin diye TTL'i yenile.
            httpContext.Response.Cookies.Append(ChallengeCookieName, challengeToken.ToString("N"), ChallengeCookieOptions);
            return Results.Redirect($"/2fa/challenge?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> Send2FaCodeAsync(
        HttpContext httpContext,
        [FromForm] string method,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadChallengeCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(ChallengeCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-CHALLENGE-NOT-FOUND");
        }

        var result = await mediator.Send(new SendTwoFactorCodeCommand(challengeToken, method), cancellationToken);

        // Her iki dalda da cookie'yi tazele — kullanıcı kod girmeye devam edecek.
        httpContext.Response.Cookies.Append(ChallengeCookieName, challengeToken.ToString("N"), ChallengeCookieOptions);

        if (result.IsFailure)
        {
            return Results.Redirect($"/2fa/challenge?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        return Results.Redirect($"/2fa/challenge?info={Uri.EscapeDataString("Kod gönderildi (Development: console log).")}");
    }

    private static bool TryReadChallengeCookie(HttpContext httpContext, out Guid challengeToken)
    {
        challengeToken = Guid.Empty;
        var raw = httpContext.Request.Cookies[ChallengeCookieName];
        if (string.IsNullOrEmpty(raw)) return false;
        return Guid.TryParseExact(raw, "N", out challengeToken) || Guid.TryParse(raw, out challengeToken);
    }

    private static bool TryReadEnrollmentCookie(HttpContext httpContext, out Guid challengeToken)
    {
        challengeToken = Guid.Empty;
        var raw = httpContext.Request.Cookies[EnrollmentCookieName];
        if (string.IsNullOrEmpty(raw)) return false;
        return Guid.TryParseExact(raw, "N", out challengeToken) || Guid.TryParse(raw, out challengeToken);
    }

    private static async Task<IResult> SignInAsync(
        HttpContext httpContext,
        [FromForm] string identifier,
        [FromForm] string password,
        [FromForm] string? persona,
        [FromForm] bool? rememberMe,
        [FromServices] IMediator mediator,
        [FromServices] CleanTenant.Infrastructure.Persistence.Catalog.CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        // HTML checkbox işaretsizse form'a hiç eklenmez — null gelir.
        var remember = rememberMe ?? false;
        var personaSide = (persona ?? "Management").Equals("Portal", StringComparison.OrdinalIgnoreCase)
            ? PersonaSide.Portal
            : PersonaSide.Management;

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new LoginCommand(identifier, password, personaSide, ContextId: null, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            var error = result.FirstError;
            return Results.Redirect($"/login?error={Uri.EscapeDataString(error.Code)}");
        }

        var login = result.Value!;
        if (login.Status == LoginStatus.TwoFactorRequired)
        {
            var token = login.Challenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(ChallengeCookieName, token.ToString("N"), ChallengeCookieOptions);
            return Results.Redirect("/2fa/challenge");
        }

        // v0.2.2.a — System scope kullanıcısı + 2FA yok → pre-auth enrollment sayfasına
        if (login.Status == LoginStatus.EnrollmentRequired)
        {
            var token = login.EnrollmentChallenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(EnrollmentCookieName, token.ToString("N"), EnrollmentCookieOptions);
            return Results.Redirect("/2fa/enroll-pre-auth");
        }

        // İlk giriş zorunlu şifre değişimi
        if (login.Status == LoginStatus.PasswordChangeRequired)
        {
            var token = login.PasswordChangeChallenge!.ChallengeToken;
            httpContext.Response.Cookies.Append(PasswordChangeCookieName, token.ToString("N"), PasswordChangeCookieOptions);
            return Results.Redirect("/change-password");
        }

        // Success → cookie set
        var tokens = login.Tokens!;
        await SignInWithSessionAsync(httpContext, tokens, remember);

        // v0.2.10.d — Kullanıcının tercih ettiği dili yükle ve cookie'ye yaz;
        // her login'de seçtiği dil otomatik gelir. UserId JWT'nin "sub" claim'inden.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        if (Guid.TryParse(jwt.Subject, out var userId))
        {
            await ApplyUserPreferredCultureAsync(httpContext, db, userId, cancellationToken);
        }

        return Results.Redirect("/");
    }

    /// <summary>
    /// v0.2.10.d — Login sonrası kullanıcının <c>PreferredCulture</c> alanını
    /// DB'den okur ve <c>.AspNetCore.Culture</c> cookie'sini ona göre set eder.
    /// Null ise sistem varsayılanı (TR) zaten kullanılır (cookie set'lenmez).
    /// </summary>
    private static async Task ApplyUserPreferredCultureAsync(
        HttpContext httpContext,
        CleanTenant.Infrastructure.Persistence.Catalog.CatalogDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var preferred = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferredCulture)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(preferred)) return;

        httpContext.Response.Cookies.Append(".AspNetCore.Culture",
            $"c={preferred}|uic={preferred}",
            new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(365),
                IsEssential = true,
            });
    }

    private static async Task<IResult> SwitchTenantFormAsync(
        HttpContext httpContext,
        [FromForm] Guid tenantId,
        [FromForm] Guid? companyId,
        [FromForm] string? returnUrl,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new SwitchTenantCommand(tenantId, companyId, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            // Hata durumunda kullanıcıyı geldiği yere geri yolla (error query ile)
            var fallback = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var sep = fallback.Contains('?') ? '&' : '?';
            return Results.Redirect($"{fallback}{sep}switch-error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        // Önce eski cookie'yi sil (yeni session id ile değişti) sonra yeni cookie set
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);

        // v0.2.13.d — Bağlam değiştiğinde her zaman ilgili bağlamın dashboard'ına
        // ("/") dön. Dashboard yeni bağlam claim'lerine göre içerik render eder.
        // returnUrl yalnız hata durumunda (kullanıcı geldiği yerde kalsın diye)
        // kullanılır.
        return Results.Redirect("/");
    }

    private static async Task<IResult> SwitchToSystemFormAsync(
        HttpContext httpContext,
        [FromForm] string? returnUrl,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var result = await mediator.Send(new SwitchToSystemCommand(ip, ua), cancellationToken);

        if (result.IsFailure)
        {
            var fallback = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
            var sep = fallback.Contains('?') ? '&' : '?';
            return Results.Redirect($"{fallback}{sep}switch-error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);

        // v0.2.13.d — System bağlamına geçişte de dashboard'a ("/") dön (yukarıdaki
        // SwitchTenantFormAsync ile aynı davranış). returnUrl yalnız hata fallback'i.
        return Results.Redirect("/");
    }

    /// <summary>
    /// v0.2.10.d — Dil tercihi değiştirme. <c>.AspNetCore.Culture</c> cookie'sini
    /// server-side set eder ve giriş yapmış kullanıcının
    /// <c>User.PreferredCulture</c> alanını günceller. Anonim kullanıcılar için
    /// yalnız cookie set'lenir (login sonrası DB değeriyle senkronlanır).
    /// </summary>
    private static async Task<IResult> ChangeCultureFormAsync(
        HttpContext httpContext,
        [FromForm] string culture,
        [FromForm] string? returnUrl,
        [FromServices] CleanTenant.Infrastructure.Persistence.Catalog.CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        // Cookie set — RequestLocalizationMiddleware sonraki istekte okur.
        var cookieValue = $"c={culture}|uic={culture}";
        httpContext.Response.Cookies.Append(".AspNetCore.Culture", cookieValue, new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(365),
            IsEssential = true,
        });

        // Login yapmışsa DB'de User.PreferredCulture'a yaz (sonraki oturumlarda hatırlanır).
        var userIdClaim = httpContext.User.FindFirst(UserIdClaim)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user is not null && user.PreferredCulture != culture)
            {
                user.PreferredCulture = culture;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    private static async Task<IResult> CompletePasswordChangeAsync(
        HttpContext httpContext,
        [FromForm] string newPassword,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var raw = httpContext.Request.Cookies[PasswordChangeCookieName];
        if (string.IsNullOrEmpty(raw) || (!Guid.TryParseExact(raw, "N", out var challengeToken) && !Guid.TryParse(raw, out challengeToken)))
        {
            httpContext.Response.Cookies.Delete(PasswordChangeCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-020");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        var command = new CompletePasswordChangeCommand(challengeToken, newPassword, ip, ua);

        var result = await mediator.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            httpContext.Response.Cookies.Append(PasswordChangeCookieName, raw, PasswordChangeCookieOptions);
            return Results.Redirect($"/change-password?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(PasswordChangeCookieName, DeleteCookieOptions);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> ForgotPasswordAsync(
        HttpContext httpContext,
        [FromForm] string email,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.Redirect("/forgot-password?error=AUTH-033");

        await mediator.Send(new RequestPasswordResetCommand(email.Trim()), cancellationToken);

        // Enumeration koruması: her zaman reset-password sayfasına yönlendir.
        httpContext.Response.Cookies.Append(
            ForgotPasswordEmailCookieName,
            email.Trim().ToLowerInvariant(),
            ForgotPasswordCookieOptions);

        return Results.Redirect("/reset-password");
    }

    private static async Task<IResult> ResetPasswordAsync(
        HttpContext httpContext,
        [FromForm] string code,
        [FromForm] string newPassword,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var email = httpContext.Request.Cookies[ForgotPasswordEmailCookieName];
        if (string.IsNullOrEmpty(email))
        {
            httpContext.Response.Cookies.Delete(ForgotPasswordEmailCookieName, DeleteCookieOptions);
            return Results.Redirect("/forgot-password?error=AUTH-034");
        }

        var command = new ResetPasswordWithCodeCommand(email, code, newPassword);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            httpContext.Response.Cookies.Append(ForgotPasswordEmailCookieName, email, ForgotPasswordCookieOptions);
            return Results.Redirect($"/reset-password?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(ForgotPasswordEmailCookieName, DeleteCookieOptions);
        return Results.Redirect("/login?info=password-reset-success");
    }

    private static async Task<IResult> FinalizePreAuthEnrollmentAsync(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!TryReadEnrollmentCookie(httpContext, out var challengeToken))
        {
            httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
            return Results.Redirect("/login?error=AUTH-2FA-ENROLL-CHALLENGE-NOT-FOUND");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua = httpContext.Request.Headers.UserAgent.ToString();

        var result = await mediator.Send(
            new FinalizePreAuthEnrollmentCommand(challengeToken, ip, ua),
            cancellationToken);

        if (result.IsFailure)
        {
            // Finalize başarısız → cookie sil; kullanıcı baştan login akışına yönlensin.
            httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
            return Results.Redirect($"/login?error={Uri.EscapeDataString(result.FirstError.Code)}");
        }

        httpContext.Response.Cookies.Delete(EnrollmentCookieName, DeleteCookieOptions);
        await SignInWithSessionAsync(httpContext, result.Value!, rememberMe: false);
        return Results.Redirect("/");
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext httpContext,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        // Backend logout — Redis session sil + refresh token revoke
        await mediator.Send(new LogoutCommand(), cancellationToken);
        // Cookie sil
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    /// <summary>
    /// Tokens üzerinden cookie SignInAsync — claim'ler SessionLookupMiddleware
    /// için (sid, ctx, scope). "Beni hatırla" 7 gün TTL.
    /// </summary>
    internal static async Task SignInWithSessionAsync(
        HttpContext httpContext,
        TokenPair tokens,
        bool rememberMe)
    {
        // v0.2.3.c — UserId'i JWT'nin "sub" claim'inden çek (TokenPair'da UserId
        // field'ı yok). NameIdentifier'a SessionId yazmak yanlıştı (önceki bug);
        // standart davranış UserId'i NameIdentifier'a, SessionId'i SidClaim'e taşır.
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwt = jwtHandler.ReadJwtToken(tokens.AccessToken);
        var userIdValue = jwt.Subject; // "sub" claim

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userIdValue),
            new(UserIdClaim, userIdValue),
            new(SidClaim, tokens.SessionId.ToString("N")),
            new(ContextIdClaim, tokens.ContextId.ToString("N")),
            new(ScopeClaim, tokens.CurrentScope.Level.ToString()),
        };

        // v0.2.3.b — Aktif bağlam claim'leri (UI gösterimi için). Blazor Server
        // SignalR circuit'inde HttpUserContext.Current null olduğundan AppBar
        // bağlamı claim'lerden okur.
        if (tokens.CurrentScope.TenantId is { } tenantId)
        {
            claims.Add(new Claim(TenantIdClaim, tenantId.ToString("N")));
            if (!string.IsNullOrEmpty(tokens.CurrentScope.TenantName))
            {
                claims.Add(new Claim(TenantNameClaim, tokens.CurrentScope.TenantName));
            }
        }
        if (tokens.CurrentScope.CompanyId is { } companyId)
        {
            claims.Add(new Claim(CompanyIdClaim, companyId.ToString("N")));
            if (!string.IsNullOrEmpty(tokens.CurrentScope.CompanyName))
            {
                claims.Add(new Claim(CompanyNameClaim, tokens.CurrentScope.CompanyName));
            }
        }
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : null,
            AllowRefresh = true,
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);
    }
}
