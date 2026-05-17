using CleanTenant.SharedKernel.Common.Errors;

namespace CleanTenant.SharedKernel.Common.Results;

/// <summary>
/// <para>
/// <see cref="Result"/>'in generic versiyonu. Başarılı sonuç bir <typeparamref name="T"/>
/// değer taşır; başarısız sonuçta <see cref="Value"/> default'tur.
/// </para>
/// <para>
/// <b>Örtük dönüşümler:</b>
/// <list type="bullet">
///   <item><c>Result&lt;User&gt; r = user;</c> — değer doğrudan başarı sonucuna çevrilir.</item>
///   <item><c>Result&lt;User&gt; r = Error.NotFound(...);</c> — hata doğrudan başarısızlık sonucuna çevrilir.</item>
/// </list>
/// Handler içinde kontrol akışı temiz olur.
/// </para>
/// </summary>
/// <typeparam name="T">Başarılı sonucun taşıdığı değer tipi.</typeparam>
public sealed class Result<T> : Result
{
    /// <summary>
    /// Başarılı sonuçta taşınan değer. Başarısız sonuçta <c>default(T)</c>.
    /// <see cref="Result.IsSuccess"/> kontrol edilmeden okunmamalıdır.
    /// </summary>
    public T? Value { get; }

    private Result(T value) : base(isSuccess: true, errors: [])
    {
        Value = value;
    }

    private Result(IReadOnlyList<Error> errors) : base(isSuccess: false, errors)
    {
        Value = default;
    }

    /// <summary>Verilen değerle başarılı sonuç üretir.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Tek hata ile başarısız sonuç üretir.</summary>
    public static new Result<T> Failure(Error error) => new([error]);

    /// <summary>Birden çok hata ile başarısız sonuç üretir.</summary>
    public static new Result<T> Failure(IEnumerable<Error> errors)
        => new(errors.ToList().AsReadOnly());

    /// <summary>
    /// Verilen <see cref="Result"/>'in hata listesini taşıyan yeni bir
    /// başarısız <see cref="Result{T}"/> üretir. Tip dönüşümünde kullanılır.
    /// </summary>
    public static Result<T> Failure(Result other)
    {
        if (other.IsSuccess)
        {
            throw new InvalidOperationException(
                "Başarılı Result'tan Failure üretilemez.");
        }
        return new Result<T>(other.Errors);
    }

    /// <summary>Değerden örtük başarı sonucu üretimi.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Hatadan örtük başarısızlık sonucu üretimi.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}
