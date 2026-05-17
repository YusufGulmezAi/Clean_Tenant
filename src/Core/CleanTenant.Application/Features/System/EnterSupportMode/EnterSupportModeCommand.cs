using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.EnterSupportMode;

/// <summary>
/// System operatörü tarafından bir tenant'a Support Mode'da giriş isteği.
/// Operatörün mevcut System scope session'ı korunur; yeni bir Support session
/// (yeni sessionId + yeni JWT) oluşturulur. Exit'te orijinal session'a dönülür.
/// </summary>
/// <param name="TargetTenantId">Hedef tenant.</param>
/// <param name="Reason">Zorunlu sebep (min 20 karakter); SupportSession kaydında saklanır.</param>
/// <param name="IpAddress">İstemci IP'si.</param>
/// <param name="UserAgent">İstemci User-Agent.</param>
public sealed record EnterSupportModeCommand(
    Guid TargetTenantId,
    string Reason,
    string IpAddress,
    string UserAgent) : IRequest<Result<TokenPair>>;
