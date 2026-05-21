using CleanTenant.SharedKernel.Context;

namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Kullanıcı detay verisi — form yüklemesi ve düzenleme için kullanılır.
/// </summary>
public record UserDetail(
    Guid Id,
    string UrlCode,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsLocked,
    bool TwoFactorEnabled,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<UserRoleAssignmentDetail> Assignments);

/// <summary>
/// Kullanıcının bir scope'taki tek rol ataması.
/// </summary>
public record UserRoleAssignmentDetail(
    Guid AssignmentId,
    Guid RoleId,
    string RoleName,
    ScopeLevel Scope,
    bool IsActive);
