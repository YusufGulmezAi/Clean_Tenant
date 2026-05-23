using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;

/// <summary>
/// Kullanıcının hesabındaki e-posta adresine, e-postayı 2FA yöntemi olarak
/// doğrulamak için tek kullanımlık kod gönderir.
/// </summary>
public sealed record SendEmailVerificationCodeCommand : IRequest<Result>;
