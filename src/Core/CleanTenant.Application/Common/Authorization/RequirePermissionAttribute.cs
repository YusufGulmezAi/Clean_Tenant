namespace CleanTenant.Application.Common.Authorization;

/// <summary>
/// <para>
/// Command/Query sınıflarına eklenerek MediatR <c>AuthorizationBehavior</c>
/// tarafından okunur. En az bir permission kodu gerektirir; çoklu kodlar
/// <b>OR</b> semantiği taşır (any-of).
/// </para>
/// <para>
/// AND semantiği (hepsi gerekli) v0.1.7+'da ihtiyaç olursa ayrı bir attribute
/// (örn. <c>RequirePermissionAllAttribute</c>) ile eklenecek.
/// </para>
/// </summary>
/// <example>
/// <code>
/// [RequirePermission("Tenant.Read", "Tenant.Manage")]
/// public sealed record GetTenantsQuery : IRequest&lt;Result&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    /// <summary>Gereken permission kodları (any-of).</summary>
    public IReadOnlyList<string> Permissions { get; }

    /// <summary>Permission kodlarıyla yeni bir requirement bildirir.</summary>
    public RequirePermissionAttribute(params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length == 0)
        {
            throw new ArgumentException("En az bir permission kodu gerekli.", nameof(permissions));
        }
        Permissions = permissions;
    }
}
