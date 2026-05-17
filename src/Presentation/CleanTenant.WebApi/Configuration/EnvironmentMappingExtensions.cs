using Microsoft.Extensions.Configuration;

namespace CleanTenant.WebApi.Configuration;

/// <summary>
/// <c>.env</c>'den gelen düz UPPER_CASE değişkenleri ASP.NET Core
/// konfigürasyon section'larına (Jwt:*, Session:*) eşleyen extension.
/// </summary>
public static class EnvironmentMappingExtensions
{
    /// <summary>
    /// CleanTenant'a özel <c>JWT_*</c> ve <c>SESSION_*</c> env değişkenlerini
    /// section formatına çevirir ve in-memory provider olarak ekler.
    /// </summary>
    public static IConfigurationBuilder AddCleanTenantEnvironmentMappings(
        this IConfigurationManager configuration)
    {
        var mappings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = configuration["JWT_ISSUER"],
            ["Jwt:Audience"] = configuration["JWT_AUDIENCE"],
            ["Jwt:SigningKey"] = configuration["JWT_SIGNING_KEY"],
            ["Jwt:AccessTokenMinutes"] = configuration["JWT_ACCESS_TOKEN_MINUTES"],
            ["Jwt:RefreshTokenDays"] = configuration["JWT_REFRESH_TOKEN_DAYS"],
            ["Session:TtlPaddingMinutes"] = configuration["SESSION_TTL_PADDING_MINUTES"],
        };
        return configuration.AddInMemoryCollection(mappings);
    }
}
