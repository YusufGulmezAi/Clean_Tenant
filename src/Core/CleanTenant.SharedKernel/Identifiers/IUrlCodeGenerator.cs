namespace CleanTenant.SharedKernel.Identifiers;

/// <summary>
/// <para>
/// <see cref="Entities.IHasUrlCode"/> implement eden entity'ler için
/// 9 karakterlik kısa URL kodu üreten servisin sözleşmesidir.
/// </para>
/// <para>
/// DI'a singleton olarak kaydedilir. Üretim algoritması Base58 alfabesidir
/// (görsel olarak karışan <c>0/O/I/l</c> karakterleri hariç).
/// </para>
/// </summary>
public interface IUrlCodeGenerator
{
    /// <summary>
    /// 9 karakterlik benzersiz (yüksek olasılıkla) bir URL kodu üretir.
    /// DB seviyesindeki unique constraint çakışmaya karşı son güvencedir;
    /// çakışma durumunda <c>SaveChangesInterceptor</c> retry yapar.
    /// </summary>
    string Generate();
}
