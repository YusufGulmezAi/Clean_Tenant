using CleanTenant.Application.Features.Main.BuildingSchema.Excel;
using CleanTenant.Domain.Tenant.BuildingSchema;
using ClosedXML.Excel;

namespace CleanTenant.Infrastructure.Export.BuildingSchema;

/// <summary>
/// <see cref="IBuildingSchemaExcelService"/>'in ClosedXML tabanlı implementasyonu.
/// Şablon üretimi (talimat + kolon rehberi + veri doğrulama dropdown'ları) ve
/// import parsing/satır-bazlı validasyon sorumluluğunu üstlenir.
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
        ("Dükkan",        BuildingType.Shop),
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

    // Etiket↔değer eşlemesi merkezî katalogdan (Domain) gelir — tek kaynak.
    private static readonly (string Label, ApartmentLayout Value)[] LayoutMap =
        ApartmentLayoutExtensions.Ordered.Select(x => (x.Label, x.Value)).ToArray();

    // ── Sütun indisleri (1-tabanlı) ─────────────────────────────────────────

    private const int ColLandName      = 1;   // Ada
    private const int ColParcelName    = 2;   // Parsel
    private const int ColBuildingName  = 3;   // Yapı
    private const int ColBuildingType  = 4;   // Yapı Tipi (liste)
    private const int ColMunicipalNo   = 5;   // Belediye No (opsiyonel)
    private const int ColBlockName     = 6;   // Blok (opsiyonel)
    private const int ColUnitNumber    = 7;   // BB No
    private const int ColUnitType      = 8;   // BB Tipi (liste)
    private const int ColSquareMeters  = 9;   // m²
    private const int ColLandShare     = 10;  // Arsa Payı
    private const int ColAllocatedArea = 11;  // Tahsis Alanı
    private const int ColOrientation   = 12;  // Yön (liste)
    private const int ColFloor         = 13;  // Kat
    private const int ColLayout        = 14;  // Oda/Salon (liste)
    private const int ColError         = 15;  // HATA (import çıktısı)
    private const int ColCount         = 15;

    // Talimat bloğu en üstte; veri tablosu başlığın HEMEN altından başlar
    // (örnek/gri satır yok — kullanıcının ilk satırı atlanmasın diye).
    private const int HeaderRow  = 27;
    private const int DataStart  = 28;
    private const int DataEnd    = 10000;

    // ── Şablon üretimi ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public byte[] GenerateTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Şablon");
        var ls = wb.Worksheets.Add("Geçerli Değerler");

        FillListSheet(ls);
        // Liste sayfası GÖRÜNÜR bırakılır — kullanıcı geçerli değerleri referans alabilir.

        AddInstructions(ws);
        AddHeaders(ws);
        AddDataValidations(ws, ls);
        SetColumnWidths(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void FillListSheet(IXLWorksheet ls)
    {
        // 1. satır = kolon başlıkları; geçerli değerler 2. satırdan itibaren.
        string[] titles = ["Yapı Tipi", "BB Tipi", "Yön", "Oda/Salon"];
        for (int c = 0; c < titles.Length; c++)
        {
            var h = ls.Cell(1, c + 1);
            h.Value = titles[c];
            h.Style.Font.Bold = true;
            h.Style.Font.FontColor = XLColor.White;
            h.Style.Fill.BackgroundColor = XLColor.FromArgb(31, 73, 125);
            h.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ls.Column(c + 1).Width = 18;
        }

        for (int i = 0; i < BuildingTypeMap.Length; i++)
            ls.Cell(i + 2, 1).Value = BuildingTypeMap[i].Label;

        for (int i = 0; i < UnitTypeMap.Length; i++)
            ls.Cell(i + 2, 2).Value = UnitTypeMap[i].Label;

        for (int i = 0; i < OrientationMap.Length; i++)
            ls.Cell(i + 2, 3).Value = OrientationMap[i].Label;

        for (int i = 0; i < LayoutMap.Length; i++)
            ls.Cell(i + 2, 4).Value = LayoutMap[i].Label;
    }

    // Görünür talimat bloğu: başlık + genel kurallar + kolon rehberi tablosu.
    private static void AddInstructions(IXLWorksheet ws)
    {
        // Başlık
        var title = ws.Cell(1, 1);
        title.Value = "BAĞIMSIZ BÖLÜM (BB) TOPLU YÜKLEME ŞABLONU";
        ws.Range(1, 1, 1, ColCount).Merge();
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        title.Style.Font.FontColor = XLColor.White;
        title.Style.Fill.BackgroundColor = XLColor.FromArgb(31, 73, 125);
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 22;

        // Genel kurallar
        var notes = new[]
        {
            "Verileri aşağıdaki BAŞLIK satırının HEMEN altındaki ilk satırdan itibaren girin (boş satır bırakmayın).",
            "Zorunlu kolonlar: Ada, Parsel, Yapı, BB No ve m². (m² 0'dan büyük olmalıdır.)",
            "Blok opsiyoneldir — boş bırakılırsa Bağımsız Bölüm doğrudan Yapı altına eklenir.",
            "Yapı Tipi, BB Tipi, Yön ve Oda/Salon hücrelerinde sağ kenardaki oktan açılır listeden SEÇİM yapın.",
            "Sayısal kolonlar: m² ve Tahsis Alanı ondalıklı olabilir; Arsa Payı ve Kat tam sayıdır.",
            "Satır SIRASI önemlidir — yükleme sırası tüm liste ve raporlarda aynen korunur.",
            "Aynı Yapı (veya aynı Blok) içinde BB No tekrar edemez. Hatalı satırlar yüklemede en sağda kırmızı 'HATA' kolonuyla işaretlenir.",
        };
        int r = 3;
        foreach (var note in notes)
        {
            var cell = ws.Cell(r, 1);
            cell.Value = "•  " + note;
            ws.Range(r, 1, r, ColCount).Merge();
            cell.Style.Alignment.WrapText = false;
            cell.Style.Font.FontColor = XLColor.FromArgb(60, 60, 60);
            r++;
        }

        // Kolon rehberi başlığı
        const int guideTitleRow = 11;
        var guideTitle = ws.Cell(guideTitleRow, 1);
        guideTitle.Value = "KOLON REHBERİ";
        ws.Range(guideTitleRow, 1, guideTitleRow, 4).Merge();
        guideTitle.Style.Font.Bold = true;
        guideTitle.Style.Font.FontColor = XLColor.White;
        guideTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(84, 130, 53);

        // Rehber tablo başlığı
        int gh = guideTitleRow + 1; // 12
        string[] guideHeaders = ["Kolon", "Zorunlu", "Tip", "Açıklama / Geçerli Değerler"];
        for (int c = 0; c < guideHeaders.Length; c++)
        {
            var cell = ws.Cell(gh, c + 1);
            cell.Value = guideHeaders[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(226, 239, 218);
        }

        // Rehber satırları
        var guide = new (string Col, string Required, string Type, string Desc)[]
        {
            ("Ada",              "Evet",  "Metin",     "Ada adı/no (en fazla 100 karakter)"),
            ("Parsel",           "Evet",  "Metin",     "Parsel adı/no (en fazla 100 karakter)"),
            ("Yapı",             "Evet",  "Metin",     "Yapı/bina adı (en fazla 200 karakter)"),
            ("Yapı Tipi",        "Evet",  "Liste",     "Konut, Konut+İşyeri, AVM, Ofis, Depo, Dükkan, Diğer"),
            ("Belediye No",      "Hayır", "Metin",     "Yapının belediye/kapı no'su (opsiyonel, max 50)"),
            ("Blok",             "Hayır", "Metin",     "Blok/kule adı (örn. A Blok). Boş = doğrudan Yapı altı"),
            ("BB No",            "Evet",  "Metin",     "Bağımsız bölüm no (en fazla 20 karakter)"),
            ("BB Tipi",          "Evet",  "Liste",     "Daire, Ofis, Dükkan, Mağaza, Depo, Otopark, Sığınak, Diğer"),
            ("m²",               "Evet",  "Sayı",      "Brüt alan; 0'dan büyük, ondalık olabilir (örn. 85,5)"),
            ("Arsa Payı",        "Evet",  "Tam sayı",  "0 veya üzeri"),
            ("Tahsis Alanı (m²)","Hayır", "Sayı",      "Balkon/teras vb. (boş veya 0 olabilir)"),
            ("Yön",              "Hayır", "Liste",     "Belirsiz, Kuzey, Güney, Doğu, Batı, KuzeyDoğu, KuzeyBatı, GüneyDoğu, GüneyBatı"),
            ("Kat",              "Evet",  "Tam sayı",  "Bodrum için negatif olabilir"),
            ("Oda/Salon",        "Hayır", "Liste",     "Bilinmiyor, Stüdyo (1+0), 1+1, ... 10 üzeri (boş = Bilinmiyor)"),
        };
        int gr = gh + 1; // 13
        foreach (var g in guide)
        {
            ws.Cell(gr, 1).Value = g.Col;
            ws.Cell(gr, 2).Value = g.Required;
            ws.Cell(gr, 3).Value = g.Type;
            ws.Cell(gr, 4).Value = g.Desc;
            if (string.Equals(g.Required, "Evet", StringComparison.Ordinal))
                ws.Cell(gr, 2).Style.Font.FontColor = XLColor.FromArgb(192, 0, 0);
            gr++;
        }
    }

    private static void AddHeaders(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "Ada", "Parsel", "Yapı", "Yapı Tipi", "Belediye No",
            "Blok", "BB No", "BB Tipi", "m²",
            "Arsa Payı", "Tahsis Alanı (m²)", "Yön", "Kat", "Oda/Salon",
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

    private static void AddDataValidations(IXLWorksheet ws, IXLWorksheet ls)
    {
        // Başlık satırı (1) hariç, değerler 2. satırdan başlar.
        SetListValidation(ws, ColBuildingType, ls.Range(2, 1, BuildingTypeMap.Length + 1, 1));
        SetListValidation(ws, ColUnitType,     ls.Range(2, 2, UnitTypeMap.Length + 1,     2));
        SetListValidation(ws, ColOrientation,  ls.Range(2, 3, OrientationMap.Length + 1,  3));
        SetListValidation(ws, ColLayout,       ls.Range(2, 4, LayoutMap.Length + 1,       4));
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
        ws.Column(ColLandName).Width      = 12;
        ws.Column(ColParcelName).Width    = 12;
        ws.Column(ColBuildingName).Width  = 18;
        ws.Column(ColBuildingType).Width  = 16;
        ws.Column(ColMunicipalNo).Width   = 14;
        ws.Column(ColBlockName).Width     = 14;
        ws.Column(ColUnitNumber).Width    = 10;
        ws.Column(ColUnitType).Width      = 14;
        ws.Column(ColSquareMeters).Width  = 10;
        ws.Column(ColLandShare).Width     = 12;
        ws.Column(ColAllocatedArea).Width = 18;
        ws.Column(ColOrientation).Width   = 14;
        ws.Column(ColFloor).Width         = 8;
        ws.Column(ColLayout).Width        = 14;
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
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? HeaderRow;

        for (int r = DataStart; r <= lastRow; r++)
        {
            var landName     = ws.Cell(r, ColLandName).GetString().Trim();
            var parcelName   = ws.Cell(r, ColParcelName).GetString().Trim();
            var buildingName = ws.Cell(r, ColBuildingName).GetString().Trim();
            var municipalNo  = ws.Cell(r, ColMunicipalNo).GetString().Trim();
            var blockName    = ws.Cell(r, ColBlockName).GetString().Trim();
            var unitNumber   = ws.Cell(r, ColUnitNumber).GetString().Trim();

            // Tamamen boş satır → atla (blok opsiyonel, kontrole dahil değil)
            if (string.IsNullOrEmpty(landName) && string.IsNullOrEmpty(parcelName)
                && string.IsNullOrEmpty(buildingName) && string.IsNullOrEmpty(unitNumber))
                continue;

            var errors = new List<string>();

            if (string.IsNullOrEmpty(landName) || landName.Length > 100)
                errors.Add("Ada zorunlu (max 100).");
            if (string.IsNullOrEmpty(parcelName) || parcelName.Length > 100)
                errors.Add("Parsel zorunlu (max 100).");
            if (string.IsNullOrEmpty(buildingName) || buildingName.Length > 200)
                errors.Add("Yapı zorunlu (max 200).");
            if (municipalNo.Length > 50)
                errors.Add("Belediye No en fazla 50 karakter olmalı.");
            if (blockName.Length > 100)
                errors.Add("Blok en fazla 100 karakter olmalı.");
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
                if (TryGetDecimal(allocCell, out var alloc) && alloc >= 0)
                    allocatedArea = alloc;
                else
                    errors.Add("Tahsis Alanı 0 veya daha büyük olmalı.");
            }

            Orientation orientation = Orientation.Unknown;
            var orientLabel = ws.Cell(r, ColOrientation).GetString().Trim();
            if (!string.IsNullOrEmpty(orientLabel) && !orientationByLabel.TryGetValue(orientLabel, out orientation))
                errors.Add($"Yön geçersiz: '{orientLabel}'.");

            int floor = 0;
            if (!TryGetInt(ws.Cell(r, ColFloor), out floor))
                errors.Add("Kat sayı olmalı.");

            // Boş bırakılırsa "Bilinmiyor" (Unknown) atanır; dolu ama listede yoksa hata.
            ApartmentLayout layout = ApartmentLayout.Unknown;
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
                    LandName     = landName,
                    ParcelName   = parcelName,
                    BuildingName = buildingName,
                    BuildingType = buildingType,
                    MunicipalNo  = string.IsNullOrEmpty(municipalNo) ? null : municipalNo,
                    BlockName    = string.IsNullOrEmpty(blockName) ? null : blockName,
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
