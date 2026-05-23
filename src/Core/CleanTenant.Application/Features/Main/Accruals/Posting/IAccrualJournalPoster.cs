using CleanTenant.Domain.Tenant.Accruals;

namespace CleanTenant.Application.Features.Main.Accruals.Posting;

/// <summary>
/// <para>
/// Bir <see cref="Accrual"/> için otomatik yevmiye fişi (Posted) üreten servis.
/// Karar 2026-05-22 (B): tahakkuk üretildiğinde fiş otomatik açılır.
/// </para>
/// <para>
/// Fiş <b>Bütçe × Dönem = 1 fiş</b> mantığıyla 2 satırdır: Borç 120.0X.NNN /
/// Alacak 600.0X.NNN, toplam tutar. BB-bazlı kırılım yevmiyeye GİRMEZ
/// (yardımcı defter <see cref="AccrualDetail"/>'de).
/// </para>
/// <para>
/// Fişi + EntrySequence artışını context'e ekler, <c>accrual.JournalEntryId</c>'yi
/// set eder; <b>SaveChanges çağırmaz</b> (çağıran handler atomik kaydeder).
/// </para>
/// </summary>
public interface IAccrualJournalPoster
{
    /// <summary>
    /// Tahakkuk için Posted yevmiye fişi üretir. Hesap kodları eksikse veya dönem
    /// kapalıysa hata döner. Başarıda fiş id'sini döner ve <c>accrual.JournalEntryId</c> set eder.
    /// </summary>
    Task<Result<Guid>> PostAsync(Accrual accrual, CancellationToken cancellationToken);
}
