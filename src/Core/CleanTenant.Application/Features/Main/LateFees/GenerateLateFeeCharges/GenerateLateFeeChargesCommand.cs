using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.LateFees.GenerateLateFeeCharges;

/// <summary>
/// <para>
/// Bir şirket için <paramref name="AsOfDate"/> itibarıyla vadesi geçmiş açık
/// anapara detaylarına gecikme faizi tahakkuğu üretir (KMK m.20 tavanlı basit faiz).
/// </para>
/// <para>
/// İdempotent/incremental: her BB için hesaplanan toplam gecikmeden halihazırda
/// işlenmiş gecikme düşülür; yalnız fark (delta &gt; 0) yeni tahakkuk olur. Aynı
/// tarihle tekrar çalıştırmak yeni borç üretmez. Hangfire günlük job yerine manuel
/// tetikleme (FAZ 6.8 ertelendi).
/// </para>
/// </summary>
[RequirePermission("tenant.accrual.generate")]
public sealed record GenerateLateFeeChargesCommand(
    Guid TenantId,
    Guid CompanyId,
    DateOnly AsOfDate) : IRequest<Result<LateFeeChargeResult>>;

/// <summary>Gecikme faizi üretim sonucu.</summary>
public sealed record LateFeeChargeResult(
    int ChargedUnitCount,
    int AccrualCount,
    decimal TotalLateFeeAmount);
