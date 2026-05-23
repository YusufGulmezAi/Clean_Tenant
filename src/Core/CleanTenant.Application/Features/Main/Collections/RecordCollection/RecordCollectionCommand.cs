using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Collections.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.RecordCollection;

/// <summary>
/// <para>
/// Bir BB'den alınan ödemeyi kaydeder. Tutar, TBK m.101 uyarınca vadesi en eski
/// açık tahakkuk detaylarından başlayarak dağıtılır (kısmi ödeme desteklenir).
/// Otomatik Posted yevmiye fişi açılır: Borç Kasa/Banka / Alacak 120.0X.NNN
/// (alacak hesabı bazında gruplanır).
/// </para>
/// <para>
/// MVP: Tutar toplam açık borçtan fazla olamaz (avans/fazla ödeme Wave 2). Aşarsa
/// COL-005 döner.
/// </para>
/// </summary>
[RequirePermission("tenant.collection.record")]
public sealed record RecordCollectionCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    Guid AccountingPeriodId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    Guid CashAccountCodeId,
    string? Reference = null,
    string? Description = null) : IRequest<Result<CollectionResult>>;

/// <summary>Tahsilat kayıt sonucu.</summary>
public sealed record CollectionResult(
    Guid CollectionId,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    int AllocationCount);
