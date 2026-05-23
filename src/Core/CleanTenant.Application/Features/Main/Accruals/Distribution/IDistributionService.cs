using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.Features.Main.Accruals.Distribution;

/// <summary>
/// <para>
/// Bir toplam tutarı Bağımsız Bölümlere dağıtan servis. Dağıtım modeline
/// (<see cref="DistributionModel"/>) göre eşit veya m² oranlı böler ve LRM
/// (Largest Remainder Method) ile yuvarlar — kuruş kaybı olmaz, payların
/// toplamı daima girdi tutarına eşittir.
/// </para>
/// <para>MVP: <see cref="DistributionModel.Equal"/> + <see cref="DistributionModel.BySquareMeter"/>.</para>
/// </summary>
public interface IDistributionService
{
    /// <summary>
    /// Toplam tutarı verilen BB'lere dağıtır. Sonuç payları toplamı = <paramref name="totalAmount"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Desteklenmeyen model (ByLandShare/Formula MVP'de yok).</exception>
    IReadOnlyList<UnitShare> Distribute(
        DistributionModel model,
        decimal totalAmount,
        IReadOnlyList<DistributionUnit> units);
}

/// <summary>Dağıtıma giren BB — kimlik + brüt m² (m² modeli için).</summary>
public sealed record DistributionUnit(Guid UnitId, decimal GrossSquareMeters);

/// <summary>BB'nin dağıtım sonucu payı.</summary>
public sealed record UnitShare(Guid UnitId, decimal Amount, decimal Share);
