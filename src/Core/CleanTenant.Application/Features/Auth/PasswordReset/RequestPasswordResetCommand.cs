using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.PasswordReset;

/// <summary>
/// Kullanıcının e-posta adresine OTP gönderir. Enumeration saldırılarına
/// karşı kullanıcı bulunmasa da Result.Success döner.
/// </summary>
public sealed record RequestPasswordResetCommand(string Email) : IRequest<Result>;
