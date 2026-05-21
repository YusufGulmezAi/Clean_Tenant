using CleanTenant.Application.Common.Authorization;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Yeni System scope kullanıcısı oluşturur ve en az bir System rolü atar.
/// Oluşturulan kullanıcı ilk girişte 2FA enrollment'a yönlendirilir
/// (AUTH-2FA-ENROLLMENT-REQUIRED akışı zaten mevcuttur).
/// </summary>
[RequirePermission("System.Users.Manage")]
public sealed record CreateSystemUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IReadOnlyList<Guid> RoleIds) : IRequest<Result<UserListItem>>;
