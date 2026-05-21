using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.PasswordReset;

/// <summary>
/// OTP kodu + yeni şifre ile şifre sıfırlar.
/// </summary>
public sealed record ResetPasswordWithCodeCommand(
    string Email,
    string Code,
    string NewPassword) : IRequest<Result>;
