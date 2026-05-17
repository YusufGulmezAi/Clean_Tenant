using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth 2FA enrollment akışının ilk adımı. Challenge token'la kullanıcının
/// TOTP authenticator key'i (yoksa) üretilir ve QR otpauth URI döner.
/// Authentication YOKTUR — Challenge token tek başına yetkilendirme aracıdır.
/// </summary>
/// <param name="ChallengeToken">Login sonucu dönen enrollment challenge token'ı.</param>
public sealed record StartPreAuthEnrollmentQuery(Guid ChallengeToken) : IRequest<Result<StartPreAuthEnrollmentResult>>;

/// <summary>
/// Start cevabı. <see cref="Email"/> kullanıcıya gösterilir, <see cref="Secret"/>
/// manuel girilebilir alternatif, <see cref="QrCodeUri"/> ise QR kod çizimi için.
/// </summary>
/// <param name="Email">Kullanıcı e-postası (sayfada gösterim).</param>
/// <param name="Secret">Base32 secret (authenticator app'lerin manuel giriş alanı).</param>
/// <param name="QrCodeUri">otpauth://totp/... URI; QR kod üretiminde kullanılır.</param>
public sealed record StartPreAuthEnrollmentResult(string Email, string Secret, string QrCodeUri);
