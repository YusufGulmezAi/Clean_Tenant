namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>Rol oluşturma / düzenleme formu binding modeli.</summary>
public sealed class RoleFormModel
{
    public string Name { get; set; } = string.Empty;
    public int Scope { get; set; }
    public string? Description { get; set; }
}
