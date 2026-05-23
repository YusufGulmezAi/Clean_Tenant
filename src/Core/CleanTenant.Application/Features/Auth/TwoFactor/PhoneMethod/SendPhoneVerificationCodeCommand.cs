using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PhoneMethod;

/// <summary>
/// Verilen telefon numarasına, telefonu 2FA yöntemi olarak doğrulamak için
/// tek kullanımlık SMS kodu gönderir. Numara bu adımda kalıcı kaydedilmez;
/// onay (<see cref="ConfirmPhoneVerificationCommand"/>) ile yazılır.
/// </summary>
/// <param name="Phone">Doğrulanacak telefon numarası (TR cep).</param>
public sealed record SendPhoneVerificationCodeCommand(string Phone) : IRequest<Result>;
