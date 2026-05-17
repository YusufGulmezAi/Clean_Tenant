using System.Reflection;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// MediatR pipeline behavior'ları için TResponse tipinde
/// <see cref="Result"/> veya <see cref="Result{T}"/> failure üretmeyi
/// kolaylaştıran reflection helper. Tüm command/query'ler bu iki tipten
/// birini döndüğü için pratiktir.
/// </summary>
internal static class ResultFactoryHelper
{
    /// <summary>
    /// <typeparamref name="TResponse"/> = <c>Result</c> veya <c>Result&lt;T&gt;</c>
    /// için verilen hata listesiyle failure üretir.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TResponse"/> <see cref="Result"/> türevi değilse.
    /// </exception>
    public static TResponse CreateFailure<TResponse>(IEnumerable<Error> errors)
    {
        var errorList = errors as IReadOnlyList<Error> ?? errors.ToList();

        var responseType = typeof(TResponse);
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errorList);
        }

        // Result<T>.Failure(IEnumerable<Error>) — generic static method
        var failureMethod = responseType.GetMethod(
            nameof(Result.Failure),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(IEnumerable<Error>)],
            modifiers: null);

        if (failureMethod is null)
        {
            throw new InvalidOperationException(
                $"Response tipi '{responseType.FullName}' Result veya Result<T> değil; " +
                "pipeline behavior bu tip için failure üretemez.");
        }

        return (TResponse)failureMethod.Invoke(null, [errorList])!;
    }
}
