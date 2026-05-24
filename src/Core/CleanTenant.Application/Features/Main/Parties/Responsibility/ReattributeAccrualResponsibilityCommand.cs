using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Parties.Responsibility;

/// <summary>
/// Bir BB'nin bütçe tahakkuk detaylarının gün-bazlı sorumluluk parçalarını, güncel
/// tenure'a göre yeniden hesaplar (tenure değişimi sonrası). <b>GUARD:</b> tahsilatı
/// olan veya o BB'de gecikme faizi bulunan dönemler ATLANIR (sessiz düzeltme yapılmaz;
/// resmi "Düzeltme &amp; Avans" işlemi gerekir). Borç toplamı ve tahsilatlar dokunulmaz.
/// </summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record ReattributeAccrualResponsibilityCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId) : IRequest<Result<ReattributeResult>>;

/// <summary>Yeniden yansıtma sonucu.</summary>
/// <param name="Recomputed">Yeniden hesaplanan (temiz) tahakkuk detayı sayısı.</param>
/// <param name="Skipped">Ödeme/gecikme nedeniyle atlanan (resmi düzeltme gereken) detay sayısı.</param>
public sealed record ReattributeResult(int Recomputed, int Skipped);
