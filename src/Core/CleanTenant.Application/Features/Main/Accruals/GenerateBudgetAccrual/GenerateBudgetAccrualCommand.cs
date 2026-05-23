using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.GenerateBudgetAccrual;

/// <summary>
/// <para>
/// Bir bütçe için belirli bir döneme (Yıl, Ay) tahakkuk üretir. Aktif bütçe
/// versiyonunun kalemlerini dönemin tutarına çevirir, BB'lere dağıtır (her kalemin
/// dağıtım modeli + katılım grubu + muafiyet kurallarıyla), kalem paylarını BB
/// başına toplar ve tek bir <c>Accrual</c> (+ BB başına <c>AccrualDetail</c>) yazar.
/// </para>
/// <para>
/// İlk tahakkukta bütçenin 120/600 alt hesapları otomatik üretilir
/// (<c>IAccountCodeAllocator</c>). Yevmiye fişi postingi Slice 6.5b'de.
/// </para>
/// <para>
/// İdempotency (Karar B): aynı (Bütçe, Dönem) için tahakkuk varsa — tahsilat yoksa
/// <see cref="Force"/>=true ile silinip yenilenir; aksi halde reddedilir.
/// </para>
/// </summary>
[RequirePermission("tenant.accrual.generate")]
public sealed record GenerateBudgetAccrualCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid BudgetId,
    int Year,
    int Month,
    bool Force = false) : IRequest<Result<AccrualResult>>;

/// <summary>Tahakkuk üretim sonucu özeti.</summary>
public sealed record AccrualResult(
    Guid AccrualId,
    decimal TotalAmount,
    int DetailCount);
