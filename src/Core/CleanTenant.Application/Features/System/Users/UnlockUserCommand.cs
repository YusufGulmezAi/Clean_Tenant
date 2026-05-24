using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kilitli bir kullanıcının hesap kilidini açar (admin aksiyonu): kilit bitiş
/// zamanını temizler ve hatalı deneme sayacını sıfırlar. <c>System.Users.Manage</c>
/// veya <c>User.Lockout</c> izni gerekir — böylece TenantAdmin kendi tenant'ındaki
/// kullanıcıların kilidini açabilir.
/// </summary>
[RequirePermission("System.Users.Manage", "User.Lockout")]
public sealed record UnlockUserCommand(string UrlCode) : IRequest<Result>;
