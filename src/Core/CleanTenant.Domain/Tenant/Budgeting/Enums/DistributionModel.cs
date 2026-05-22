namespace CleanTenant.Domain.Tenant.Budgeting.Enums;

/// <summary>
/// <para>
/// Bütçe kalemi tutarının Bağımsız Bölümlere nasıl dağıtılacağını belirleyen
/// model. Tahakkuk üretim motoru (FAZ 6) bu değere bakarak ilgili dağıtım
/// motorunu çağırır (<c>IDistributionEngine</c> implementasyonları).
/// </para>
/// <para>
/// LRM (Largest Remainder Method) yuvarlama her dağıtımdan sonra uygulanır;
/// böylece toplam tutar dağıtılan paylar arasında kuruş kaybı olmadan eşitlenir.
/// </para>
/// <para>
/// <b>MVP kapsamı:</b> <see cref="Equal"/> ve <see cref="BySquareMeter"/>
/// implement edilir. Diğer değerler enum'da yerini alır (gelecekte aktive
/// edilecek), ama FAZ 6 öncesi seçilirse validation reddeder.
/// </para>
/// </summary>
public enum DistributionModel
{
    /// <summary>Eşit dağılım: <c>tutar / BBCount</c>. KMK m.18 default eşit paylaşım.</summary>
    Equal = 0,

    /// <summary>Brüt m² oranlı: <c>tutar × (BB.GrossM² / Toplam.GrossM²)</c>.</summary>
    BySquareMeter = 1,

    /// <summary>Arsa payı oranlı (Wave 2+).</summary>
    ByLandShare = 2,

    /// <summary>Oda sayısı oranlı (Wave 2+).</summary>
    ByRoomCount = 3,

    /// <summary>Özel formül; <c>DistributionConfig</c> alanında parametreler (Wave 3+).</summary>
    Formula = 99
}
