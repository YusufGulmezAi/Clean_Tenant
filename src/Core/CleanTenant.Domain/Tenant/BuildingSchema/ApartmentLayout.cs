namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Bağımsız bölümün oda + salon kombinasyonu (Türkiye'deki "oda+salon" gösterimi).
/// Sıralama (0..44) doğrudan UI'daki açılır liste sırasını belirler;
/// görüntüleme etiketleri <see cref="ApartmentLayoutExtensions"/>'tadır.
/// Yarım odalar (örn. 1.5+1) ayrı üye olarak tutulur.
/// </summary>
public enum ApartmentLayout
{
    /// <summary>Bilinmiyor / belirtilmemiş. Excel'de Oda/Salon boş geldiğinde varsayılan;
    /// negatif değer açılır listede en başta görünmesini sağlar.</summary>
    Unknown = -1,

    /// <summary>Stüdyo (1+0).</summary>
    Studio = 0,

    /// <summary>1+1.</summary>
    OnePlusOne = 1,

    /// <summary>1.5+1.</summary>
    OneHalfPlusOne = 2,

    /// <summary>2+0.</summary>
    TwoPlusZero = 3,

    /// <summary>2+1.</summary>
    TwoPlusOne = 4,

    /// <summary>2.5+1.</summary>
    TwoHalfPlusOne = 5,

    /// <summary>2+2.</summary>
    TwoPlusTwo = 6,

    /// <summary>3+0.</summary>
    ThreePlusZero = 7,

    /// <summary>3+1.</summary>
    ThreePlusOne = 8,

    /// <summary>3.5+1.</summary>
    ThreeHalfPlusOne = 9,

    /// <summary>3+2.</summary>
    ThreePlusTwo = 10,

    /// <summary>3+3.</summary>
    ThreePlusThree = 11,

    /// <summary>4+0.</summary>
    FourPlusZero = 12,

    /// <summary>4+1.</summary>
    FourPlusOne = 13,

    /// <summary>4.5+1.</summary>
    FourHalfPlusOne = 14,

    /// <summary>4.5+2.</summary>
    FourHalfPlusTwo = 15,

    /// <summary>4+2.</summary>
    FourPlusTwo = 16,

    /// <summary>4+3.</summary>
    FourPlusThree = 17,

    /// <summary>4+4.</summary>
    FourPlusFour = 18,

    /// <summary>5+1.</summary>
    FivePlusOne = 19,

    /// <summary>5.5+1.</summary>
    FiveHalfPlusOne = 20,

    /// <summary>5+2.</summary>
    FivePlusTwo = 21,

    /// <summary>5+3.</summary>
    FivePlusThree = 22,

    /// <summary>5+4.</summary>
    FivePlusFour = 23,

    /// <summary>6+1.</summary>
    SixPlusOne = 24,

    /// <summary>6+2.</summary>
    SixPlusTwo = 25,

    /// <summary>6.5+1.</summary>
    SixHalfPlusOne = 26,

    /// <summary>6+3.</summary>
    SixPlusThree = 27,

    /// <summary>6+4.</summary>
    SixPlusFour = 28,

    /// <summary>7+1.</summary>
    SevenPlusOne = 29,

    /// <summary>7+2.</summary>
    SevenPlusTwo = 30,

    /// <summary>7+3.</summary>
    SevenPlusThree = 31,

    /// <summary>8+1.</summary>
    EightPlusOne = 32,

    /// <summary>8+2.</summary>
    EightPlusTwo = 33,

    /// <summary>8+3.</summary>
    EightPlusThree = 34,

    /// <summary>8+4.</summary>
    EightPlusFour = 35,

    /// <summary>9+1.</summary>
    NinePlusOne = 36,

    /// <summary>9+2.</summary>
    NinePlusTwo = 37,

    /// <summary>9+3.</summary>
    NinePlusThree = 38,

    /// <summary>9+4.</summary>
    NinePlusFour = 39,

    /// <summary>9+5.</summary>
    NinePlusFive = 40,

    /// <summary>9+6.</summary>
    NinePlusSix = 41,

    /// <summary>10+1.</summary>
    TenPlusOne = 42,

    /// <summary>10+2.</summary>
    TenPlusTwo = 43,

    /// <summary>10 ve üzeri.</summary>
    OverTen = 44,
}
