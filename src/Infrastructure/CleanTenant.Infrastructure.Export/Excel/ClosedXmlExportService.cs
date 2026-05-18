using System.Globalization;
using ClosedXML.Excel;
using CleanTenant.Application.Common.Export;

namespace CleanTenant.Infrastructure.Export.Excel;

/// <summary>
/// <para>
/// <see cref="IExcelExportService"/>'in ClosedXML (MIT) implementasyonu.
/// Tek sheet, başlık bold + AutoFilter aktif, kolonlar AutoFit.
/// </para>
/// </summary>
public sealed class ClosedXmlExportService : IExcelExportService
{
    /// <inheritdoc />
    public byte[] Generate<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string title)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(title);
        if (columns.Count == 0)
        {
            throw new ArgumentException("En az bir kolon gerekli.", nameof(columns));
        }

        using var workbook = new XLWorkbook();
        var sheetName = Sanitize(title);
        var ws = workbook.Worksheets.Add(sheetName);

        // Header
        for (int c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Rows
        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var col = columns[c];
                var value = col.Selector(row!);
                WriteCell(ws.Cell(rowIndex, c + 1), value, col.Format);
            }
            rowIndex++;
        }

        // AutoFilter + AutoFit
        var dataRange = ws.Range(1, 1, Math.Max(1, rowIndex - 1), columns.Count);
        dataRange.SetAutoFilter();
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteCell(IXLCell cell, object? value, string? format)
    {
        if (value is null)
        {
            cell.Value = string.Empty;
            return;
        }

        switch (value)
        {
            case string s:
                cell.Value = s;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                if (format is not null) cell.Style.NumberFormat.Format = format;
                break;
            case DateTimeOffset dto:
                cell.Value = dto.DateTime;
                if (format is not null) cell.Style.NumberFormat.Format = format;
                break;
            case DateOnly dOnly:
                cell.Value = dOnly.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = format ?? "yyyy-MM-dd";
                break;
            case Guid g:
                cell.Value = g.ToString();
                break;
            case IFormattable f when format is not null:
                cell.Value = f.ToString(format, CultureInfo.InvariantCulture);
                break;
            case decimal dec:
                cell.Value = dec;
                if (format is not null) cell.Style.NumberFormat.Format = format;
                break;
            case double dbl:
                cell.Value = dbl;
                if (format is not null) cell.Style.NumberFormat.Format = format;
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            default:
                cell.Value = value.ToString();
                break;
        }
    }

    /// <summary>Sheet adı 31 char + ()/[]\/?* gibi yasak karakterler.</summary>
    private static string Sanitize(string title)
    {
        var clean = new string(title.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray());
        return clean.Length > 31 ? clean[..31] : (clean.Length == 0 ? "Sheet1" : clean);
    }
}
