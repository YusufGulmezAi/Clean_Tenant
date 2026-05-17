using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.ImpersonateUser;

/// <summary>
/// Support Mode'da hedef kullanıcıya bürünme (Full Impersonation). Hedef
/// kullanıcının destek session'ın hedef tenant'ında aktif ataması olmalı.
/// Yeni session: JWT <c>sub</c> hedef kullanıcı, <c>ImpersonatedBy</c> = operatör.
/// </summary>
/// <param name="TargetUserUrlCode">Hedef kullanıcının URL kodu.</param>
/// <param name="Reason">Zorunlu sebep (min 20 karakter).</param>
/// <param name="IpAddress">İstemci IP'si.</param>
/// <param name="UserAgent">İstemci User-Agent.</param>
public sealed record ImpersonateUserCommand(
    string TargetUserUrlCode,
    string Reason,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
