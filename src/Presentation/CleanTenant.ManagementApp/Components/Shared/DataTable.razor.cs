using CleanTenant.Application.Common.Export;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <para>
/// <see cref="DataTable{TItem}"/>'in code-behind sınıfı. Generic Blazor component
/// olarak MudDataGrid'i sarar; export (Excel/PDF) + multi-keyword AND quick
/// filter + toolbar slot eklentileri sağlar.
/// </para>
/// <para>
/// <b>CascadingTypeParameter</b>: Caller <c>&lt;PropertyColumn&gt;</c> /
/// <c>&lt;TemplateColumn&gt;</c> yazdığında inner MudDataGrid'in <c>T</c>
/// parametresi <c>TItem</c>'dan otomatik inference olur — açıkça <c>T="..."</c>
/// yazılmasına gerek kalmaz.
/// </para>
/// <para>
/// <b>Quick filter</b>: Boşlukla ayrılmış kelimelerin <b>tümü</b> bir satırın
/// herhangi bir property string'inde bulunmalıdır (AND semantiği). Caller
/// <see cref="QuickFilter"/> ile özel filtre tanımlarsa o ek olarak AND'lenir.
/// </para>
/// <para>
/// <b>Export</b>: <see cref="ExcelColumns"/> / <see cref="PdfColumns"/> verilmezse
/// export butonu görünür ama tıklanınca uyarı snackbar'ı çıkar. Caller column
/// definitions verirse JS interop ile blob download tetiklenir.
/// </para>
/// </summary>
[CascadingTypeParameter(nameof(TItem))]
public sealed partial class DataTable<TItem> : ComponentBase
{
    /// <summary>Görüntülenecek satır koleksiyonu. Null ise loading spinner.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TItem>? Items { get; set; }

    /// <summary>Toolbar başlığı (boş ise gizlenir).</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Caller'ın geçtiği MudDataGrid kolonları.</summary>
    [Parameter, EditorRequired] public RenderFragment Columns { get; set; } = default!;

    /// <summary>
    /// Quick filter input'unun placeholder metni. Null/boş bırakılırsa
    /// <c>DataTable.SearchPlaceholder</c> localizasyon anahtarı kullanılır.
    /// </summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>Quick filter'ı göster/gizle (default: göster).</summary>
    [Parameter] public bool ShowQuickFilter { get; set; } = true;

    /// <summary>Caller'ın isteğe bağlı ek filtre fonksiyonu (default null).</summary>
    [Parameter] public Func<TItem, bool>? QuickFilter { get; set; }

    /// <summary>Multi-keyword AND filter için string property'leri belirler. Boşsa tüm public string property'ler taranır.</summary>
    [Parameter] public Func<TItem, IEnumerable<string?>>? SearchableFields { get; set; }

    /// <summary>Satır tıklama callback'i.</summary>
    [Parameter] public EventCallback<DataGridRowClickEventArgs<TItem>> RowClick { get; set; }

    /// <summary>Satır CSS class fonksiyonu (alternating bg vb.).</summary>
    [Parameter] public Func<TItem, int, string>? RowClassFunc { get; set; }

    /// <summary>MudDataGrid Filterable.</summary>
    [Parameter] public bool Filterable { get; set; } = true;

    /// <summary>MudDataGrid FilterMode (default ColumnFilterRow).</summary>
    [Parameter] public DataGridFilterMode FilterMode { get; set; } = DataGridFilterMode.ColumnFilterRow;

    /// <summary>MudDataGrid Hover.</summary>
    [Parameter] public bool Hover { get; set; } = true;

    /// <summary>MudDataGrid Dense.</summary>
    [Parameter] public bool Dense { get; set; } = true;

    /// <summary>MudDataGrid Groupable (drag-to-group panel).</summary>
    [Parameter] public bool Groupable { get; set; } = true;

    /// <summary>Kolon options menüsünü göster (filter/sort/group).</summary>
    [Parameter] public bool ShowColumnOptions { get; set; } = true;

    /// <summary>Tablonun yatayda kaydırılabilir olup olmadığı (default false).</summary>
    [Parameter] public bool HorizontalScrollbar { get; set; } = false;

    /// <summary>Excel export butonu göster.</summary>
    [Parameter] public bool ShowExcelExport { get; set; } = true;

    /// <summary>PDF export butonu göster.</summary>
    [Parameter] public bool ShowPdfExport { get; set; } = true;

    /// <summary>Export'ta kullanılacak kolon tanımları (Excel + PDF ortak). Null ise export butonu pasif.</summary>
    [Parameter] public IReadOnlyList<ExportColumn<TItem>>? ExportColumns { get; set; }

    /// <summary>Sadece Excel için kolonlar (override).</summary>
    [Parameter] public IReadOnlyList<ExportColumn<TItem>>? ExcelColumns { get; set; }

    /// <summary>Sadece PDF için kolonlar (override).</summary>
    [Parameter] public IReadOnlyList<ExportColumn<TItem>>? PdfColumns { get; set; }

    /// <summary>Export dosya adı için kullanılan başlık (boşsa <see cref="Title"/>).</summary>
    [Parameter] public string? ExportTitle { get; set; }

    /// <summary>NoRecords içeriği için özel render slot (boşsa default mesaj).</summary>
    [Parameter] public RenderFragment? NoRecordsContent { get; set; }

    /// <summary>Toolbar sağ taraf için ek slot (örn. "Yeni" butonu).</summary>
    [Parameter] public RenderFragment? ToolbarRight { get; set; }

    [Inject] private IExcelExportService Excel { get; set; } = default!;
    [Inject] private IPdfExportService Pdf { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IStringLocalizer Loc { get; set; } = default!;

    private static readonly CultureInfo _trCulture = CultureInfo.GetCultureInfo("tr-TR");

    private string _searchText = string.Empty;

    /// <summary>SearchPlaceholder parameter null/boş ise lokalize default'a düşer.</summary>
    private string ResolvedSearchPlaceholder => string.IsNullOrWhiteSpace(SearchPlaceholder)
        ? Loc["DataTable.SearchPlaceholder"].Value
        : SearchPlaceholder;

    /// <summary>Multi-keyword AND filter — boşlukla ayrılmış kelimelerin tümü eşleşmeli.</summary>
    private Func<TItem, bool> CombinedQuickFilter => item =>
    {
        // Caller custom filter
        if (QuickFilter is not null && !QuickFilter(item))
        {
            return false;
        }

        // Multi-keyword AND
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        var tokens = _searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return true;

        var fields = SearchableFields is not null
            ? SearchableFields(item)
            : DefaultSearchableFields(item);

        var joined = string.Join(" ", fields.Where(f => !string.IsNullOrEmpty(f)));

        return tokens.All(t => _trCulture.CompareInfo.IndexOf(joined, t, CompareOptions.IgnoreCase) >= 0);
    };

    /// <summary>Default: tüm public string + ToString sonucu — reflection kullanmaz, basit ToString.</summary>
    private static IEnumerable<string?> DefaultSearchableFields(TItem item)
    {
        yield return item?.ToString();
    }

    private async Task ExportExcelAsync()
    {
        var cols = ExcelColumns ?? ExportColumns;
        if (cols is null || cols.Count == 0)
        {
            Snackbar.Add(Loc["DataTable.Export.NoColumns.Excel"], Severity.Warning);
            return;
        }
        if (Items is null) return;

        var bytes = Excel.Generate(Items, cols, ResolveTitle());
        var fileName = $"{SanitizeFileName(ResolveTitle())}_{BuildTimestamp()}.xlsx";
        await DownloadBlobAsync(bytes, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private async Task ExportPdfAsync()
    {
        var cols = PdfColumns ?? ExportColumns;
        if (cols is null || cols.Count == 0)
        {
            Snackbar.Add(Loc["DataTable.Export.NoColumns.Pdf"], Severity.Warning);
            return;
        }
        if (Items is null) return;

        var bytes = Pdf.Generate(Items, cols, ResolveTitle());
        var fileName = $"{SanitizeFileName(ResolveTitle())}_{BuildTimestamp()}.pdf";
        await DownloadBlobAsync(bytes, fileName, "application/pdf");
    }

    private async Task DownloadBlobAsync(byte[] bytes, string fileName, string mimeType)
    {
        var base64 = Convert.ToBase64String(bytes);
        await JS.InvokeVoidAsync("cleantenant.downloadBlobBase64", fileName, base64, mimeType);
    }

    private string ResolveTitle() => string.IsNullOrWhiteSpace(ExportTitle)
        ? (Title ?? Loc["DataTable.DefaultTitle"].Value)
        : ExportTitle;

    private static string BuildTimestamp()
        => DateTimeOffset.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);

    private static string SanitizeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(title.Select(c =>
            invalid.Contains(c) ? '_' : c == ' ' ? '-' : c).ToArray());
        return string.IsNullOrEmpty(clean.Trim('-', '_')) ? "rapor" : clean.Trim('-', '_');
    }
}
