using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Refresh;

/// <summary>
/// Refresh token kullanarak yeni access token alma isteği.
/// </summary>
/// <param name="RefreshToken">İstemcide saklanan raw refresh token.</param>
/// <param name="IpAddress">İstemci IP'si (audit + replay tespiti için).</param>
/// <param name="UserAgent">İstemci tarayıcı bilgisi.</param>
public sealed record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
