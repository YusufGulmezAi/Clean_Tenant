using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>
/// SMS ile gönderilen kodu doğrular; başarılıysa numara hesaba kaydedilir ve
/// <c>PhoneNumberConfirmed=true</c> olur (telefon bir 2FA yöntemi olur).
/// </summary>
/// <param name="Phone">Kodun gönderildiği telefon numarası.</param>
/// <param name="Code">SMS'le gelen 6 haneli kod.</param>
public sealed record ConfirmPhoneVerificationCommand(string Phone, string Code) : IRequest<Result>;
