using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.EmailMethod;

/// <summary>
/// E-postaya gönderilen kodu doğrular; başarılıysa <c>EmailConfirmed=true</c>
/// olur ve e-posta bir 2FA yöntemi olarak kullanılabilir hale gelir.
/// </summary>
/// <param name="Code">E-postaya gönderilen 6 haneli kod.</param>
public sealed record ConfirmEmailVerificationCommand(string Code) : IRequest<Result>;
