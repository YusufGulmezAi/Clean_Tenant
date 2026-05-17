namespace CleanTenant.SharedKernel.Entities;

/// <summary>
/// Tüm varlıkların (entity) ortak işaretleyici arabirimi. Bir tipin domain
/// varlığı olduğunu ve <see cref="Id"/> ile kimliklendiğini bildirir.
/// EF Core'da global query filter'lar ve generic repository'ler bu arabirime
/// göre çalışır.
/// </summary>
public interface IEntity
{
    /// <summary>Entity'nin benzersiz kimliği. UUID v7 (zaman-sıralı) tercih edilir.</summary>
    Guid Id { get; }
}
