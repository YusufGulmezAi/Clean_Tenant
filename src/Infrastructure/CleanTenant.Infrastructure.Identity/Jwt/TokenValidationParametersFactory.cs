using System.Text;
using CleanTenant.Application.Common.Auth;
using Microsoft.IdentityModel.Tokens;

namespace CleanTenant.Infrastructure.Identity.Jwt;

/// <summary>
/// ASP.NET Core JwtBearer middleware'in kullanacağı
/// <see cref="TokenValidationParameters"/>'ı üretir.
/// </summary>
internal static class TokenValidationParametersFactory
{
    /// <summary>JWT bearer auth için validation parametrelerini oluşturur.</summary>
    public static TokenValidationParameters Create(JwtSettings settings)
    {
        var keyBytes = Encoding.UTF8.GetBytes(settings.SigningKey);
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            NameClaimType = JwtClaimNames.Subject,
        };
    }
}
