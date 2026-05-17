using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Auth.Logout;

/// <summary>
/// <para>
/// Logout isteği. HTTP isteğinden gelen bearer token'ın session bilgisi
/// kullanılır; ek parametre yok. Handler <see cref="SharedKernel.Context.IUserContext"/>
/// üzerinden mevcut kullanıcının session bilgisine erişir.
/// </para>
/// </summary>
public sealed record LogoutCommand : IRequest<Result>;
