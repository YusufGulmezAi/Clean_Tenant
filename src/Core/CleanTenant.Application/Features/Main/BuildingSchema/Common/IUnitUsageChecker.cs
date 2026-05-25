using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Common;

/// <summary>
/// Yapı şeması silme işlemlerinde, hedef alt-ağaçtaki Bağımsız Bölümlerin
/// sistemde başka modüllerce (tahakkuk, cari malik/kiracı/iletişim, katılım grubu,
/// muafiyet) kullanılıp kullanılmadığını denetler.
/// </summary>
public interface IUnitUsageChecker
{
    /// <summary>
    /// Verilen Bağımsız Bölümlerden herhangi biri sistemde (silinmemiş bir kayıtla)
    /// kullanılıyorsa açıklayıcı bir <see cref="Result.Failure(Error)"/>, hiçbiri
    /// kullanılmıyorsa <see cref="Result.Success()"/> döndürür. Boş kümede daima Success.
    /// </summary>
    Task<Result> EnsureUnitsDeletableAsync(IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken);
}
