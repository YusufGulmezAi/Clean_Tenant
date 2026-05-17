using CleanTenant.SharedKernel.Common.Errors;

namespace CleanTenant.SharedKernel.Common.Results;

/// <summary>
/// <para>
/// Generic olmayan Result tipidir. Bir işlemin başarı veya başarısızlık
/// sonucunu, başarısızsa hata listesini taşır. Exception fırlatmak yerine
/// "beklenen hata" durumlarında bu tip kullanılır.
/// </para>
/// <para>
/// <b>Garanti:</b> Başarılı sonuçta hata listesi boş; başarısız sonuçta en
/// az bir hata vardır. Bu invariant ctor'da zorlanır.
/// </para>
/// </summary>
public class Result
{
    /// <summary>İşlem başarılı ise true.</summary>
    public bool IsSuccess { get; }

    /// <summary>İşlem başarısız ise true (<see cref="IsSuccess"/> tersi).</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Hata listesi. Başarılı sonuçta boş.</summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    /// İlk (genelde tek) hata. Başarılı sonuçta <see cref="Error.None"/>.
    /// Tek hata bekleyen sade akışlar için pratik erişim.
    /// </summary>
    public Error FirstError => Errors.Count > 0 ? Errors[0] : Error.None;

    /// <summary>
    /// Result ctor'u; alt sınıflar tarafından çağrılır.
    /// </summary>
    /// <param name="isSuccess">Başarı durumu.</param>
    /// <param name="errors">Hata listesi. Başarılı durumda boş olmalı.</param>
    /// <exception cref="InvalidOperationException">
    /// Başarı durumunda hata varsa ya da başarısız durumda hiç hata yoksa.
    /// </exception>
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Başarılı Result, hata listesi içeremez.");
        }
        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException(
                "Başarısız Result, en az bir hata içermelidir.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>Başarılı sonuç üretir.</summary>
    public static Result Success() => new(true, []);

    /// <summary>Tek hata ile başarısız sonuç üretir.</summary>
    public static Result Failure(Error error) => new(false, [error]);

    /// <summary>Birden çok hata ile başarısız sonuç üretir.</summary>
    public static Result Failure(IEnumerable<Error> errors)
        => new(false, errors.ToList().AsReadOnly());
}
