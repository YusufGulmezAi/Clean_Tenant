using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Common;

/// <summary>
/// Yapı şeması kademeli silme handler'larının ortak soft-delete yardımcısı.
/// Manuel <c>IsDeleted=true</c> (Modified state) yolu kullanılır; FullAuditInterceptor
/// bunu Delete olarak audit'ler, AuditingInterceptor UpdatedAt/By damgalar.
/// </summary>
internal static class SoftDeleteExtensions
{
    /// <summary>Entity'yi soft-delete olarak işaretler (IsDeleted + DeletedAt).</summary>
    public static void SoftDelete(this ISoftDeletable entity, DateTimeOffset now)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = now;
    }
}
