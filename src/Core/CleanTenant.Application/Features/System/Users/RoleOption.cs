namespace CleanTenant.Application.Features.System.Users;

/// <summary>
/// Rol atama formunda gösterilen seçenek. Scope'a göre filtreli liste döner.
/// </summary>
public record RoleOption(Guid Id, string Name, string? Description, bool IsBuiltIn);
