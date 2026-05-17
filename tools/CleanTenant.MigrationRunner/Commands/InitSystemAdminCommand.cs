using System.CommandLine;
using CleanTenant.Domain.Identity.Authorization;
using CleanTenant.Domain.Identity.Users;
using CleanTenant.Infrastructure.Persistence.Catalog;
using CleanTenant.MigrationRunner.Infrastructure;
using CleanTenant.SharedKernel.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CleanTenant.MigrationRunner.Commands;

/// <summary>
/// <c>init-system-admin</c> alt komutu. Production deploy sonrası bir
/// kez çalıştırılır; tek bir Developer System kullanıcı oluşturur.
/// Şifre interaktif olarak prompt edilir (CLI history'ye düşmez).
/// </summary>
internal static class InitSystemAdminCommand
{
    /// <summary>System.CommandLine için komut tanımını üretir.</summary>
    public static Command Build()
    {
        var envOption = new Option<string>("--env")
        {
            Description = "Hedef ortam.",
            Required = true,
        };
        var emailOption = new Option<string>("--email")
        {
            Description = "Admin kullanıcının e-postası.",
            Required = true,
        };
        var firstNameOption = new Option<string>("--first-name")
        {
            Description = "Admin kullanıcının adı.",
            Required = true,
        };
        var lastNameOption = new Option<string>("--last-name")
        {
            Description = "Admin kullanıcının soyadı.",
            Required = true,
        };

        var command = new Command(
            "init-system-admin",
            "Production bootstrap: tek bir Developer System kullanıcı oluşturur.")
        {
            envOption, emailOption, firstNameOption, lastNameOption,
        };

        command.SetAction(async parseResult =>
        {
            var env = parseResult.GetValue(envOption)!;
            var email = parseResult.GetValue(emailOption)!;
            var firstName = parseResult.GetValue(firstNameOption)!;
            var lastName = parseResult.GetValue(lastNameOption)!;
            await ExecuteAsync(env, email, firstName, lastName);
            return 0;
        });

        return command;
    }

    private static async Task ExecuteAsync(
        string environment,
        string email,
        string firstName,
        string lastName)
    {
        Console.Write("Şifre (terminalde görüntülenmez): ");
        var password = ReadPassword();
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Error.WriteLine("Hata: Şifre boş geçilemez.");
            Environment.ExitCode = 1;
            return;
        }

        using var host = HostBuilderFactory.Build(environment);
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        using var scope = host.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Mevcut mu?
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            logger.LogWarning("[{Env}] Bu e-posta zaten kayıtlı: {Email}", environment, email);
            return;
        }

        // Developer rolünü bul
        var developerRole = await db.Roles
            .AsNoTracking()
            .Where(r => r.NormalizedName == "DEVELOPER" && r.Scope == ScopeLevel.System)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();
        if (developerRole == Guid.Empty)
        {
            Console.Error.WriteLine(
                "Hata: Developer (System) rolü bulunamadı. Önce 'seed --env " + environment + "' çalıştırın.");
            Environment.ExitCode = 1;
            return;
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            TwoFactorEnabled = true, // System için zorunlu (kural gereği)
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine("Hata: Admin oluşturulamadı.");
            foreach (var err in result.Errors)
            {
                Console.Error.WriteLine($"  - {err.Code}: {err.Description}");
            }
            Environment.ExitCode = 1;
            return;
        }

        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            UserId = user.Id,
            RoleId = developerRole,
            ScopeLevel = ScopeLevel.System,
            AssignedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        logger.LogInformation("[{Env}] System admin oluşturuldu: {Email}", environment, email);
        Console.WriteLine();
        Console.WriteLine("UYARI: Bu kullanıcının 2FA'sını etkinleştirmek için ilk login'de Authenticator app ekleyin.");
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;
        do
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password = password[..^1];
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
            }
        } while (key.Key != ConsoleKey.Enter);
        Console.WriteLine();
        return password;
    }
}
