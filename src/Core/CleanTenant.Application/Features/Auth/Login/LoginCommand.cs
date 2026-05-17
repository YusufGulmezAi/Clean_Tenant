using CleanTenant.Application.Common.Auth;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Login;

/// <summary>
/// <para>
/// Kullanıcı login isteği. <c>Identifier</c> tek alan — server email / TCKN /
/// YKN / VKN / cep telefonu tipini otomatik tespit eder
/// (<see cref="LoginIdentifier"/>).
/// </para>
/// <para>
/// <see cref="Persona"/> zorunlu güvenlik sınırı: Management persona'sı yalnız
/// System / Tenant / Company scope'larını görür; Portal persona'sı yalnız Unit.
/// </para>
/// </summary>
/// <param name="Identifier">Email, TCKN/YKN (11 hane), VKN (10 hane) veya telefon.</param>
/// <param name="Password">Düz metin şifre.</param>
/// <param name="Persona">Login tarafı — ManagementApp ya Management, PortalApp ya Portal.</param>
/// <param name="ContextId">
/// Sekme/persona context kimliği. İstemci null gönderebilir; sunucu üretir.
/// </param>
/// <param name="IpAddress">İstemci IP'si (audit + login bildirimi için).</param>
/// <param name="UserAgent">İstemci tarayıcı / uygulama bilgisi.</param>
public sealed record LoginCommand(
    string Identifier,
    string Password,
    PersonaSide Persona,
    Guid? ContextId,
    string IpAddress,
    string UserAgent) : IRequest<Result<LoginResult>>;
