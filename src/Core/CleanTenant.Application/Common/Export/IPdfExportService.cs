namespace CleanTenant.Application.Common.Export;

/// <summary>
/// <para>
/// Bir satır koleksiyonunu PDF byte dizisine serialize eder. Implementation
/// QuestPDF (2023.12 community license, ücretsiz) ile.
/// </para>
/// <para>
/// <b>Davranış</b>: A4 landscape, header (başlık + tarih), tablo (alternating
/// row colors), footer (sayfa numarası). v0.2.4.a'da basit B&amp;W; logo/header
/// v0.2.5+'da eklenir.
/// </para>
/// </summary>
public interface IPdfExportService
{
    /// <summary>PDF byte dizisini döner.</summary>
    /// <typeparam name="T">Satır tipi.</typeparam>
    /// <param name="rows">Sözlük satır koleksiyonu.</param>
    /// <param name="columns">Kolon tanımları (sıra önemli).</param>
    /// <param name="title">PDF dökümanı başlığı (header'da yazılır).</param>
    /// <returns>.pdf dosyasının ham byte içeriği.</returns>
    byte[] Generate<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title);
}
