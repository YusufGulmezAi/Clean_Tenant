namespace CleanTenant.Application.Common.Export;

/// <summary>
/// <para>
/// Bir satır koleksiyonunu Excel (.xlsx) byte dizisine serialize eder.
/// Implementation ClosedXML (MIT) ile.
/// </para>
/// <para>
/// <b>Davranış</b>: tek sheet; ilk satır başlık (bold + AutoFilter aktif);
/// veri satırları AutoFit; <c>Title</c> sheet adı + footer olarak kullanılır.
/// </para>
/// </summary>
public interface IExcelExportService
{
    /// <summary>Excel byte dizisini döner.</summary>
    /// <typeparam name="T">Satır tipi.</typeparam>
    /// <param name="rows">Sözlük satır koleksiyonu.</param>
    /// <param name="columns">Kolon tanımları (sıra önemli).</param>
    /// <param name="title">Sheet adı + dosya adında kullanılan başlık. Max 31 char (Excel sınırı).</param>
    /// <returns>.xlsx dosyasının ham byte içeriği.</returns>
    byte[] Generate<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title);
}
