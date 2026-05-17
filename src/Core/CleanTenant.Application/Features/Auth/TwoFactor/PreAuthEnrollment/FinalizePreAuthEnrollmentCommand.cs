using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.TwoFactor.PreAuthEnrollment;

/// <summary>
/// Pre-auth enrollment akışının son adımı. Challenge doğrulanmış olmalı
/// (<see cref="PreAuthEnrollmentChallenge.VerifiedAt"/> set); aksi takdirde
/// reddedilir. <c>LoginFinalizer</c> ile <c>TokenPair</c> üretilir ve
/// challenge tüketilir.
/// </summary>
/// <param name="ChallengeToken">Pre-auth enrollment challenge token.</param>
/// <param name="IpAddress">İstemci IP (audit).</param>
/// <param name="UserAgent">İstemci User-Agent (audit).</param>
public sealed record FinalizePreAuthEnrollmentCommand(
    Guid ChallengeToken,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
