using System.Globalization;
using CleanTenant.Application.Common.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CleanTenant.Infrastructure.Export.Pdf;

/// <summary>
/// <para>
/// <see cref="IPdfExportService"/>'in QuestPDF (2023.12 community license)
/// implementasyonu. A4 landscape, header (başlık + tarih), tablo (alternating
/// row colors), footer (sayfa numarası).
/// </para>
/// </summary>
public sealed class QuestPdfExportService : IPdfExportService
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

        var rowList = rows.ToList();
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, title, rowList.Count));
                page.Content().Element(c => ComposeTable(c, columns, rowList));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Sayfa ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" / ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title, int rowCount)
    {
        container.Column(col =>
        {
            col.Item().Text(title).FontSize(16).Bold();
            col.Item().Row(row =>
            {
                row.RelativeItem().Text($"Toplam Satır: {rowCount}").FontSize(9);
                row.ConstantItem(150).AlignRight().Text(
                    DateTimeOffset.UtcNow.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")))
                    .FontSize(9);
            });
            col.Item().PaddingTop(8).LineHorizontal(0.5f);
        });
    }

    private static void ComposeTable<T>(IContainer container, IReadOnlyList<ExportColumn<T>> columns, List<T> rows)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var col in columns)
                {
                    if (col.RelativeWidth > 0)
                    {
                        cols.RelativeColumn(col.RelativeWidth);
                    }
                    else
                    {
                        cols.RelativeColumn();
                    }
                }
            });

            // Header
            table.Header(header =>
            {
                foreach (var col in columns)
                {
                    header.Cell()
                        .Background(Colors.Grey.Lighten2)
                        .Padding(4)
                        .Text(col.Header).Bold();
                }
            });

            // Rows (alternating bg)
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                var bg = r % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                foreach (var col in columns)
                {
                    var value = col.Selector(row!);
                    var text = Format(value, col.Format);
                    table.Cell()
                        .Background(bg)
                        .Padding(3)
                        .Text(text);
                }
            }
        });
    }

    private static string Format(object? value, string? format)
    {
        if (value is null) return string.Empty;
        return value switch
        {
            string s => s,
            DateTime dt when format is not null => dt.ToString(format, CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTimeOffset dto when format is not null => dto.ToString(format, CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            DateOnly dOnly => dOnly.ToString(format ?? "yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f when format is not null => f.ToString(format, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
