using CleanTenant.SharedKernel.Common.Errors;
using FluentValidation;
using MediatR;

namespace CleanTenant.Application.Common.Pipeline;

/// <summary>
/// <para>
/// Authorization sonrası, handler öncesi çalışır. <see cref="IValidator{T}"/>
/// implement eden tüm validator'lar request üzerinde çalıştırılır; <b>tüm
/// ihlaller toplanır</b> (form için kullanıcı dostu) ve <c>Result.Failure</c>
/// olarak döner. Handler tetiklenmez.
/// </para>
/// <para>
/// Hiç validator yoksa pass-through. Hata kodu validator'da
/// <c>WithErrorCode("AUTH-001")</c> şeklinde verilmediyse <c>VAL-001</c> default.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next().ConfigureAwait(false);
        }

        var errors = failures
            .Select(f => Error.Validation(
                string.IsNullOrWhiteSpace(f.ErrorCode) ? "VAL-001" : f.ErrorCode,
                f.ErrorMessage))
            .ToList();

        return ResultFactoryHelper.CreateFailure<TResponse>(errors);
    }
}
