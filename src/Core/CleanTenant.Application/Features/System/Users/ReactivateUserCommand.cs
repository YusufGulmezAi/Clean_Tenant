using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Devre dışı bırakılmış kullanıcıyı tekrar aktif eder.
/// </summary>
[RequirePermission("System.Users.Manage")]
public sealed record ReactivateUserCommand(string UrlCode) : IRequest<Result>;
