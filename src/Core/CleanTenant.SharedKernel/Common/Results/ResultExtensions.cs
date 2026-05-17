using CleanTenant.SharedKernel.Common.Errors;

namespace CleanTenant.SharedKernel.Common.Results;

/// <summary>
/// <see cref="Result"/> ve <see cref="Result{T}"/> için işlevsel
/// kompozisyon helper'ları. Handler kodunu zincirleme yazıma izin verir.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Başarılı sonuçtaki değeri verilen fonksiyonla başka tipe dönüştürür.
    /// Başarısız sonuçta hata listesi aynen yeni Result'a taşınır.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }
        return Result<TOut>.Success(mapper(result.Value!));
    }

    /// <summary>
    /// Başarılı sonuçtaki değer üzerinden yeni bir Result üreten fonksiyonu
    /// uygular. Başarısız sonuçta hata listesi taşınır. Monad-style "and-then".
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }
        return binder(result.Value!);
    }

    /// <summary>
    /// Result'ın iki dalını farklı fonksiyonlarla işler ve tek tip değer döndürür.
    /// Genellikle endpoint cevabını üretirken kullanılır.
    /// </summary>
    public static TResponse Match<TIn, TResponse>(
        this Result<TIn> result,
        Func<TIn, TResponse> onSuccess,
        Func<IReadOnlyList<Error>, TResponse> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.IsSuccess ? onSuccess(result.Value!) : onFailure(result.Errors);
    }
}
