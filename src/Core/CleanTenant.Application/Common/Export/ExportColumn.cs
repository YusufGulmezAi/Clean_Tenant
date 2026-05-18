namespace CleanTenant.Application.Common.Export;

/// <summary>
/// <para>
/// Bir export (Excel/PDF) çıktısının bir kolonunu tanımlayan generic
/// projection — <c>DataTable&lt;TItem&gt;</c> kolonlarından otomatik çıkarılabilir
/// veya caller tarafından özelleştirilebilir.
/// </para>
/// <para>
/// <see cref="Selector"/> her satır için kolon değerini döndürür. Null
/// değerler export'ta boş hücre olarak basılır. <see cref="Format"/> opsiyonel
/// — değer null değilse bu string format'la <c>IFormattable</c> tipler için
/// formatlanır (örn. <c>"N2"</c>, <c>"yyyy-MM-dd"</c>).
/// </para>
/// </summary>
/// <typeparam name="T">Satır tipi.</typeparam>
public sealed class ExportColumn<T>
{
    /// <summary>Excel başlığı / PDF kolon başlığı.</summary>
    public required string Header { get; init; }

    /// <summary>Satırdan kolon değerini çeker.</summary>
    public required Func<T, object?> Selector { get; init; }

    /// <summary>IFormattable değerler için format string (örn. "N2", "yyyy-MM-dd").</summary>
    public string? Format { get; init; }

    /// <summary>
    /// PDF için kolon genişliği bilgisi (relative). 0 ise eşit dağıtım.
    /// Excel için pixel/auto-fit kullanılır, bu değer dikkate alınmaz.
    /// </summary>
    public float RelativeWidth { get; init; }
}
