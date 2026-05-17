using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth 2FA enrollment'ın ikinci adımı: kullanıcı authenticator app'in
/// verdiği 6 haneli kodu doğrular. Doğru ise:
/// <list type="number">
///   <item><c>TwoFactorEnabled=true</c>.</item>
///   <item>10 recovery code üretilir (her biri tek kullanımlık).</item>
///   <item>Challenge'ın <c>VerifiedAt</c>'i set edilir (finalize için hazır).</item>
/// </list>
/// </summary>
/// <param name="ChallengeToken">Pre-auth enrollment challenge token.</param>
/// <param name="Code">Authenticator app'in ürettiği 6 haneli TOTP kodu.</param>
public sealed record CompletePreAuthEnrollmentCommand(Guid ChallengeToken, string Code)
    : IRequest<Result<CompletePreAuthEnrollmentResult>>;

/// <summary>
/// Onay yanıtı: 10 recovery code (her biri tek kullanımlık). Yalnız bir kere
/// döner — kullanıcı kaydetmeli. Sonraki adım: finalize endpoint (cookie set).
/// </summary>
/// <param name="RecoveryCodes">10 adet tek kullanımlık kurtarma kodu.</param>
public sealed record CompletePreAuthEnrollmentResult(IReadOnlyList<string> RecoveryCodes);
