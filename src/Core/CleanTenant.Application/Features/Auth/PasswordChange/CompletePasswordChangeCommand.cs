using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.PasswordChange;

/// <summary>
/// İlk giriş zorunlu şifre değişimi akışını tamamlar.
/// Challenge token Redis'ten doğrulanır, yeni şifre set edilir,
/// RequiresPasswordChange false'a set edilir, ardından normal TokenPair üretilir.
/// </summary>
public sealed record CompletePasswordChangeCommand(
    Guid ChallengeToken,
    string NewPassword,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
