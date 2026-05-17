using CleanTenant.Application.Common.Pipeline;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CleanTenant.Application.UnitTests.Pipeline;

/// <summary>
/// <see cref="ValidationBehavior{TRequest,TResponse}"/> davranış testleri.
/// </summary>
public sealed class ValidationBehaviorTests
{
    /// <summary>Boş validator listesi → handler doğrudan çalışır.</summary>
    [Fact]
    public async Task Validator_yoksa_handler_calismali()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>(validators: []);

        var nextCalled = false;
        var response = await behavior.Handle(
            new TestRequest(""),
            () => { nextCalled = true; return Task.FromResult(Result<string>.Success("ok")); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be("ok");
    }

    /// <summary>Tüm validator hataları toplanır; handler çağrılmaz.</summary>
    [Fact]
    public async Task Validator_birden_cok_hata_uretirse_hepsi_donmeli()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(
            [
                new ValidationFailure("Value", "Hata 1") { ErrorCode = "VAL-A" },
                new ValidationFailure("Value", "Hata 2") { ErrorCode = "VAL-B" },
            ]));

        var behavior = new ValidationBehavior<TestRequest, Result<string>>([validator]);

        var nextCalled = false;
        var response = await behavior.Handle(
            new TestRequest(""),
            () => { nextCalled = true; return Task.FromResult(Result<string>.Success("ok")); },
            CancellationToken.None);

        nextCalled.Should().BeFalse();
        response.IsFailure.Should().BeTrue();
        response.Errors.Should().HaveCount(2);
        response.Errors.Should().Contain(e => e.Code == "VAL-A");
        response.Errors.Should().Contain(e => e.Code == "VAL-B");
        response.Errors.Should().AllSatisfy(e => e.Type.Should().Be(ErrorType.Validation));
    }

    /// <summary>Validator geçerli sonuç döndürürse handler çalışır.</summary>
    [Fact]
    public async Task Validator_gecerli_donerse_handler_calismali()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, Result<string>>([validator]);

        var response = await behavior.Handle(
            new TestRequest("x"),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
    }

    /// <summary>Hata kodu boş ise default <c>VAL-001</c> kullanılır.</summary>
    [Fact]
    public async Task Bos_error_code_default_VAL_001_olmali()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Value", "Boş kod")]));

        var behavior = new ValidationBehavior<TestRequest, Result<string>>([validator]);

        var response = await behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Errors[0].Code.Should().Be("VAL-001");
    }

    public sealed record TestRequest(string Value) : IRequest<Result<string>>;
}
