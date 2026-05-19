namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>Rol oluşturma / düzenleme formu binding modeli.</summary>
public sealed class RoleFormModel
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Rol kapsam seviyesi. <see cref="CleanTenant.SharedKernel.Context.ScopeLevel"/>
    /// enum'u ile eşleşir: 1=System, 2=Tenant, 3=Company, 4=Unit. 0 (None)
    /// dropdown'da görünmez; varsayılan değer 1=Sistem.
    /// </summary>
    public int Scope { get; set; } = 1;
    public string? Description { get; set; }
}
