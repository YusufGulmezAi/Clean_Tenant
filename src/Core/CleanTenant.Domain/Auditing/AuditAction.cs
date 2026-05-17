namespace CleanTenant.Domain.Auditing;

/// <summary>
/// Bir audit kaydının taşıdığı işlem tipi. EF Core'un
/// <c>EntityState</c>'inden türetilir; <c>IsDeleted=true</c> set'leyen
/// soft-delete operasyonu da <see cref="Delete"/> olarak işlenir
/// (kullanıcı algısı: silindi).
/// </summary>
public enum AuditAction
{
    /// <summary>Yeni entity yaratıldı (EF <c>Added</c>).</summary>
    Create = 1,

    /// <summary>Mevcut entity güncellendi (EF <c>Modified</c>).</summary>
    Update = 2,

    /// <summary>
    /// Entity silindi (EF <c>Deleted</c>) <b>veya</b> <c>ISoftDeletable.IsDeleted</c>
    /// false → true olarak güncellendi.
    /// </summary>
    Delete = 3,
}
