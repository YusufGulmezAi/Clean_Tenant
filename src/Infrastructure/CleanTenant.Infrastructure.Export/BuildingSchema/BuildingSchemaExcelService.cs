using CleanTenant.Application.Features.Main.BuildingSchema.Excel;
using CleanTenant.Domain.Tenant.BuildingSchema;
using ClosedXML.Excel;

namespace CleanTenant.Infrastructure.Export.BuildingSchema;

/// <summary>
/// <see cref="IBuildingSchemaExcelService"/>'in ClosedXML tabanlı implementasyonu.
/// Şablon üretimi ve import parsing/validasyon sorumluluğunu üstlenir.
/// </summary>
public sealed class BuildingSchemaExcelService : IBuildingSchemaExcelService
{
    // ── Türkçe etiket ↔ enum değer eşlemeleri ───────────────────────────────

    private static readonly (string Label, BuildingType Value)[] BuildingTypeMap =
    [
        ("Konut",         BuildingType.Residential),
        ("Konut+İşyeri",  BuildingType.ResidentialCommercial),
        ("AVM",           BuildingType.ShoppingMall),
        ("Ofis",          BuildingType.Office),
        ("Depo",          BuildingType.Warehouse),
        ("Diğer",         BuildingType.Other),
    ];

    private static readonly (string Label, UnitType Value)[] UnitTypeMap =
    [
        ("Daire",   UnitType.Apartment),
        ("Ofis",    UnitType.Office),
        ("Dükkan",  UnitType.Shop),
        ("Mağaza",  UnitType.Store),
        ("Depo",    UnitType.Storage),
        ("Otopark", UnitType.Parking),
        ("Sığınak", UnitType.Shelter),
        ("Diğer",   UnitType.Other),
    ];

    private static readonly (string Label, Orientation Value)[] OrientationMap =
    [
        ("Belirsiz",   Orientation.Unknown),
        ("Kuzey",      Orientation.North),
        ("Güney",      Orientation.South),
        ("Doğu",       Orientation.East),
        ("Batı",       Orientation.West),
        ("KuzeyDoğu",  Orientation.NorthEast),
        ("KuzeyBatı",  Orientation.NorthWest),
        ("GüneyDoğu",  Orientation.SouthEast),
        ("GüneyBatı",  Orientation.SouthWest),
    ];

    private static readonly (string Label, ApartmentLayout Value)[] LayoutMap =
    [
        ("Stüdyo", ApartmentLayout.Studio),
        ("1+0",    ApartmentLayout.OneRoom),
        ("1+1",    ApartmentLayout.OneBedroom),
        ("2+1",    ApartmentLayout.TwoBedroom),
        ("3+1",    ApartmentLayout.ThreeBedroom),
        ("4+1",    ApartmentLayout.FourBedroom),
        ("5+1",    ApartmentLayout.FiveBedroom),
        ("Diğer",  ApartmentLayout.Other),
    ];

    // ── Sütun indisleri (1-tabanlı) ─────────────────────────────────────────

    private const int ColBlockName     = 1;
    private const int ColParcelName    = 2;
    private const int ColBuildingName  = 3;
    private const int ColBuildingType  = 4;
    private const int ColUnitNumber    = 5;
    private const int ColUnitType      = 6;
    private const int ColSquareMeters  = 7;
    private const int ColLandShare     = 8;
    private const int ColAllocatedArea = 9;
    private const int ColOrientation   = 10;
    private const int ColFloor         = 11;
    private const int ColLayout        = 12;
    private const int ColError         = 13;

    private const int HeaderRow  = 1;
    private const int ExampleRow = 2;
    private const int DataStart  = 3;
    private const int DataEnd    = 10000;

    // ── Şablon üretimi ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public byte[] GenerateTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Şablon");
        var ls = wb.Worksheets.Add("_Listeler");

        FillListSheet(ls);
        ls.Visibility = XLWorksheetVisibility.Hidden;

        AddHeaders(ws);
        AddExampleRow(ws);
        AddDataValidations(ws, ls);
        SetColumnWidths(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void FillListSheet(IXLWorksheet ls)
    {
        for (int i = 0; i < BuildingTypeMap.Length; i++)
            ls.Cell(i + 1, 1).Value = BuildingTypeMap[i].Label;

        for (int i = 0; i < UnitTypeMap.Length; i++)
            ls.Cell(i + 1, 2).Value = UnitTypeMap[i].Label;

        for (int i = 0; i < OrientationMap.Length; i++)
            ls.Cell(i + 1, 3).Value = OrientationMap[i].Label;

        for (int i = 0; i < LayoutMap.Length; i++)
            ls.Cell(i + 1, 4).Value = LayoutMap[i].Label;
    }

    private static void AddHeaders(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "Ada Adı", "Parsel Adı", "Yapı Adı", "Yapı Tipi",
            "BB No",   "BB Tipi",   "m²",        "Arsa Payı",
            "Tahsis Alanı (m²)", "Yön",          "Kat",       "Oda/Salon",
        };

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(HeaderRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(31, 73, 125);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }

    private static void AddExampleRow(IXLWorksheet ws)
    {
        var examples = new object[]
        {
            "123", "1", "A Blok", "Konut",
            "1",   "Daire", 85.50m, 15,
            "",    "Güney", 2, "3+1",
        };

        for (int c = 0; c < examples.Length; c++)
        {
            var cell = ws.Cell(ExampleRow, c + 1);
            if (examples[c] is decimal d)
                cell.Value = d;
            else if (examples[c] is int i)
                cell.Value = i;
            else
                cell.Value = examples[c]?.ToString() ?? "";

            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.Gray;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
        }
    }

    private static void AddDataValidations(IXLWorksheet ws, IXLWorksheet ls)
    {
        SetListValidation(ws, ColBuildingType, ls.Range(1, 1, BuildingTypeMap.Length, 1));
        SetListValidation(ws, ColUnitType,     ls.Range(1, 2, UnitTypeMap.Length,     2));
        SetListValidation(ws, ColOrientation,  ls.Range(1, 3, OrientationMap.Length,  3));
        SetListValidation(ws, ColLayout,       ls.Range(1, 4, LayoutMap.Length,       4));
    }

    private static void SetListValidation(IXLWorksheet ws, int col, IXLRange listRange)
    {
        var colLetter = ColumnLetter(col);
        var dv = ws.Range($"{colLetter}{DataStart}:{colLetter}{DataEnd}").CreateDataValidation();
        dv.List(listRange, true);
        dv.IgnoreBlanks = true;
        dv.ShowErrorMessage = true;
        dv.ErrorTitle = "Geçersiz değer";
        dv.ErrorMessage = "Lütfen listeden bir değer seçin.";
    }

    private static void SetColumnWidths(IXLWorksheet ws)
    {
        ws.Column(ColBlockName).Width     = 14;
        ws.Column(ColParcelName).Width    = 14;
        ws.Column(ColBuildingName).Width  = 18;
        ws.Column(ColBuildingType).Width  = 16;
        ws.Column(ColUnitNumber).Width    = 10;
        ws.Column(ColUnitType).Width      = 14;
        ws.Column(ColSquareMeters).Width  = 10;
        ws.Column(ColLandShare).Width     = 12;
        ws.Column(ColAllocatedArea).Width = 18;
        ws.Column(ColOrientation).Width   = 14;
        ws.Column(ColFloor).Width         = 8;
        ws.Column(ColLayout).Width        = 12;
    }

    // ── Import parsing + validasyon ──────────────────────────────────────────

    /// <inheritdoc />
    public BuildingSchemaParseResult ParseAndValidate(Stream excelStream)
    {
        using var wb = new XLWorkbook(excelStream);
        var ws = wb.Worksheets.FirstOrDefault(s => s.Name == "Şablon")
                 ?? wb.Worksheets.First();

        var buildingTypeByLabel  = BuildingTypeMap.ToDictionary(x => x.Label, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var unitTypeByLabel      = UnitTypeMap.ToDictionary(x => x.Label, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var orientationByLabel   = OrientationMap.ToDictionary(x => x.Label, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var layoutByLabel        = LayoutMap.ToDictionary(x => x.Label, x => x.Value, StringComparer.OrdinalIgnoreCase);

        var rows    = new List<BuildingSchemaImportRow>();
        var hasAnyError = false;
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? ExampleRow;

        for (int r = DataStart; r <= lastRow; r++)
        {
            var blockName    = ws.Cell(r, ColBlockName).GetString().Trim();
            var parcelName   = ws.Cell(r, ColParcelName).GetString().Trim();
            var buildingName = ws.Cell(r, ColBuildingName).GetString().Trim();
            var unitNumber   = ws.Cell(r, ColUnitNumber).GetString().Trim();

            // Tamamen boş satır → atla
            if (string.IsNullOrEmpty(blockName) && string.IsNullOrEmpty(parcelName)
                && string.IsNullOrEmpty(buildingName) && string.IsNullOrEmpty(unitNumber))
                continue;

            var errors = new List<string>();

            if (string.IsNullOrEmpty(blockName) || blockName.Length > 100)
                errors.Add("Ada Adı zorunlu (max 100).");
            if (string.IsNullOrEmpty(parcelName) || parcelName.Length > 100)
                errors.Add("Parsel Adı zorunlu (max 100).");
            if (string.IsNullOrEmpty(buildingName) || buildingName.Length > 200)
                errors.Add("Yapı Adı zorunlu (max 200).");
            if (string.IsNullOrEmpty(unitNumber) || unitNumber.Length > 20)
                errors.Add("BB No zorunlu (max 20).");

            BuildingType buildingType = default;
            var buildingTypeLabel = ws.Cell(r, ColBuildingType).GetString().Trim();
            if (!buildingTypeByLabel.TryGetValue(buildingTypeLabel, out buildingType))
                errors.Add($"Yapı Tipi geçersiz: '{buildingTypeLabel}'.");

            UnitType unitType = default;
            var unitTypeLabel = ws.Cell(r, ColUnitType).GetString().Trim();
            if (!unitTypeByLabel.TryGetValue(unitTypeLabel, out unitType))
                errors.Add($"BB Tipi geçersiz: '{unitTypeLabel}'.");

            decimal squareMeters = 0;
            if (!TryGetDecimal(ws.Cell(r, ColSquareMeters), out squareMeters) || squareMeters <= 0)
                errors.Add("m² zorunlu ve 0'dan büyük olmalı.");

            int landShare = 0;
            if (!TryGetInt(ws.Cell(r, ColLandShare), out landShare) || landShare < 0)
                errors.Add("Arsa Payı zorunlu ve 0 veya üzeri olmalı.");

            decimal? allocatedArea = null;
            var allocCell = ws.Cell(r, ColAllocatedArea);
            if (!allocCell.IsEmpty())
            {
                if (TryGetDecimal(allocCell, out var alloc) && alloc > 0)
                    allocatedArea = alloc;
                else
                    errors.Add("Tahsis Alanı 0'dan büyük olmalı.");
            }

            Orientation orientation = Orientation.Unknown;
            var orientLabel = ws.Cell(r, ColOrientation).GetString().Trim();
            if (!string.IsNullOrEmpty(orientLabel) && !orientationByLabel.TryGetValue(orientLabel, out orientation))
                errors.Add($"Yön geçersiz: '{orientLabel}'.");

            int floor = 0;
            if (!TryGetInt(ws.Cell(r, ColFloor), out floor))
                errors.Add("Kat sayı olmalı.");

            ApartmentLayout layout = ApartmentLayout.Other;
            var layoutLabel = ws.Cell(r, ColLayout).GetString().Trim();
            if (!string.IsNullOrEmpty(layoutLabel) && !layoutByLabel.TryGetValue(layoutLabel, out layout))
                errors.Add($"Oda/Salon geçersiz: '{layoutLabel}'.");

            if (errors.Count > 0)
            {
                hasAnyError = true;
                var errorCell = ws.Cell(r, ColError);
                errorCell.Value = string.Join(" | ", errors);
                errorCell.Style.Font.FontColor = XLColor.DarkRed;
                errorCell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 199, 206);
            }
            else
            {
                rows.Add(new BuildingSchemaImportRow
                {
                    LandName     = blockName,
                    ParcelName   = parcelName,
                    BuildingName = buildingName,
                    BuildingType = buildingType,
                    UnitNumber   = unitNumber,
                    UnitType     = unitType,
                    SquareMeters = squareMeters,
                    LandShare    = landShare,
                    AllocatedArea = allocatedArea,
                    Floor        = floor,
                    Orientation  = orientation,
                    Layout       = layout,
                });
            }
        }

        if (!hasAnyError)
            return new BuildingSchemaParseResult { HasErrors = false, Rows = rows };

        // Hata başlığını ekle (henüz yoksa)
        var headerCell = ws.Cell(HeaderRow, ColError);
        if (headerCell.IsEmpty())
        {
            headerCell.Value = "HATA";
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Font.FontColor = XLColor.White;
            headerCell.Style.Fill.BackgroundColor = XLColor.DarkRed;
        }

        using var errorMs = new MemoryStream();
        wb.SaveAs(errorMs);
        return new BuildingSchemaParseResult { HasErrors = true, ErrorWorkbook = errorMs.ToArray() };
    }

    // ── Yardımcı metotlar ────────────────────────────────────────────────────

    private static bool TryGetDecimal(IXLCell cell, out decimal value)
    {
        if (cell.TryGetValue(out value)) return true;
        if (cell.TryGetValue(out double d)) { value = (decimal)d; return true; }
        if (decimal.TryParse(cell.GetString().Replace(',', '.'), System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out value)) return true;
        return false;
    }

    private static bool TryGetInt(IXLCell cell, out int value)
    {
        if (cell.TryGetValue(out value)) return true;
        if (cell.TryGetValue(out double d)) { value = (int)d; return true; }
        return int.TryParse(cell.GetString().Trim(), out value);
    }

    private static string ColumnLetter(int col)
    {
        // 1=A, 2=B, ..., 26=Z, 27=AA ...
        var result = "";
        while (col > 0)
        {
            col--;
            result = (char)('A' + col % 26) + result;
            col /= 26;
        }
        return result;
    }
}
