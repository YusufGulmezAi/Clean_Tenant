using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.ForceLogoutUser;

/// <summary>
/// Admin tarafından bir kullanıcının tüm session'larını sonlandırma isteği.
/// Endpoint'te policy kontrolü zorunlu; yalnız System ya da Tenant Admin
/// scope'undan çağrılabilir.
/// </summary>
/// <param name="TargetUserUrlCode">Hedef kullanıcının 9 karakterlik URL kodu.</param>
/// <param name="Reason">Zorunlu sebep (audit + müşteri şeffaflığı).</param>
public sealed record ForceLogoutUserCommand(
    string TargetUserUrlCode,
    string Reason) : IRequest<Result>;
