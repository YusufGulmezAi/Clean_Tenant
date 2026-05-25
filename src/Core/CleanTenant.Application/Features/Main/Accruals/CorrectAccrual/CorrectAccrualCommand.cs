using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accruals.CorrectAccrual;

/// <summary>
/// Bir tahakkuk detayını kısmen/tamamen geri alan ters kayıt (storno) üretir.
/// <para>
/// Geçmiş mutate edilmez: ayrı bir <c>AccrualSource.Correction</c> tahakkuğu
/// (NEGATİF tutarlı detay, orijinale <c>CorrectedAccrualDetailId</c> ile bağlı) +
/// ters yönlü dengeli yevmiye (Borç gelir 600 / Alacak alacak 120, pozitif tutar,
/// <c>EntryType.Correction</c>, <c>OriginalEntryId</c>) oluşturulur.
/// </para>
/// <para>
/// Net etki: BB'nin net borcu (KPI/ledger) <paramref name="Amount"/> kadar düşer.
/// Ödenmiş bir detay düzeltilirse fazla ödeme net bakiyede alacaklıya döner
/// (avansa çevrim — reattribute-with-correction, Slice 5).
/// </para>
/// </summary>
[RequirePermission("tenant.correction.execute")]
public sealed record CorrectAccrualCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid AccrualDetailId,
    decimal Amount,
    string? Reason = null) : IRequest<Result<CorrectionResult>>;

/// <summary>Ters kayıt sonucu.</summary>
public sealed record CorrectionResult(
    Guid CorrectionAccrualId,
    Guid CorrectionDetailId,
    Guid JournalEntryId,
    decimal Amount,
    bool PendingApproval);
