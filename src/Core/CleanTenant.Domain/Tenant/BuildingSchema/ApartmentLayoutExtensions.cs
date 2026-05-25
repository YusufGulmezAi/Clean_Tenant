namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// <see cref="ApartmentLayout"/> için merkezî görüntüleme etiketi kataloğu.
/// UI açılır listeleri, tablolar ve Excel parse/şablon hep buradan beslenir;
/// böylece etiket eşlemesi tek yerde tutulur. Liste sırası = enum değer sırası
/// (0..44) = açılır liste sırası.
/// </summary>
public static class ApartmentLayoutExtensions
{
    /// <summary>Tüm layout'lar, tanımlı sıralarıyla ve görüntüleme etiketleriyle.</summary>
    public static readonly IReadOnlyList<(ApartmentLayout Value, string Label)> Ordered =
    [
        (ApartmentLayout.Unknown,          "Bilinmiyor"),
        (ApartmentLayout.Studio,           "Stüdyo (1+0)"),
        (ApartmentLayout.OnePlusOne,       "1+1"),
        (ApartmentLayout.OneHalfPlusOne,   "1.5+1"),
        (ApartmentLayout.TwoPlusZero,      "2+0"),
        (ApartmentLayout.TwoPlusOne,       "2+1"),
        (ApartmentLayout.TwoHalfPlusOne,   "2.5+1"),
        (ApartmentLayout.TwoPlusTwo,       "2+2"),
        (ApartmentLayout.ThreePlusZero,    "3+0"),
        (ApartmentLayout.ThreePlusOne,     "3+1"),
        (ApartmentLayout.ThreeHalfPlusOne, "3.5+1"),
        (ApartmentLayout.ThreePlusTwo,     "3+2"),
        (ApartmentLayout.ThreePlusThree,   "3+3"),
        (ApartmentLayout.FourPlusZero,     "4+0"),
        (ApartmentLayout.FourPlusOne,      "4+1"),
        (ApartmentLayout.FourHalfPlusOne,  "4.5+1"),
        (ApartmentLayout.FourHalfPlusTwo,  "4.5+2"),
        (ApartmentLayout.FourPlusTwo,      "4+2"),
        (ApartmentLayout.FourPlusThree,    "4+3"),
        (ApartmentLayout.FourPlusFour,     "4+4"),
        (ApartmentLayout.FivePlusOne,      "5+1"),
        (ApartmentLayout.FiveHalfPlusOne,  "5.5+1"),
        (ApartmentLayout.FivePlusTwo,      "5+2"),
        (ApartmentLayout.FivePlusThree,    "5+3"),
        (ApartmentLayout.FivePlusFour,     "5+4"),
        (ApartmentLayout.SixPlusOne,       "6+1"),
        (ApartmentLayout.SixPlusTwo,       "6+2"),
        (ApartmentLayout.SixHalfPlusOne,   "6.5+1"),
        (ApartmentLayout.SixPlusThree,     "6+3"),
        (ApartmentLayout.SixPlusFour,      "6+4"),
        (ApartmentLayout.SevenPlusOne,     "7+1"),
        (ApartmentLayout.SevenPlusTwo,     "7+2"),
        (ApartmentLayout.SevenPlusThree,   "7+3"),
        (ApartmentLayout.EightPlusOne,     "8+1"),
        (ApartmentLayout.EightPlusTwo,     "8+2"),
        (ApartmentLayout.EightPlusThree,   "8+3"),
        (ApartmentLayout.EightPlusFour,    "8+4"),
        (ApartmentLayout.NinePlusOne,      "9+1"),
        (ApartmentLayout.NinePlusTwo,      "9+2"),
        (ApartmentLayout.NinePlusThree,    "9+3"),
        (ApartmentLayout.NinePlusFour,     "9+4"),
        (ApartmentLayout.NinePlusFive,     "9+5"),
        (ApartmentLayout.NinePlusSix,      "9+6"),
        (ApartmentLayout.TenPlusOne,       "10+1"),
        (ApartmentLayout.TenPlusTwo,       "10+2"),
        (ApartmentLayout.OverTen,          "10 üzeri"),
    ];

    private static readonly Dictionary<ApartmentLayout, string> LabelByValue =
        Ordered.ToDictionary(x => x.Value, x => x.Label);

    /// <summary>Layout'un görüntüleme etiketini döndürür (örn. "2.5+1").</summary>
    public static string ToDisplay(this ApartmentLayout layout) =>
        LabelByValue.TryGetValue(layout, out var label) ? label : layout.ToString();
}
