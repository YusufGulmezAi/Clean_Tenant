using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Pipeline;
using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.UnitTests.Pipeline;

/// <summary>
/// <see cref="AuthorizationBehavior{TRequest,TResponse}"/> davranış testleri.
/// </summary>
public sealed class AuthorizationBehaviorTests
{
    /// <summary>Attribute yoksa pass-through.</summary>
    [Fact]
    public async Task Attribute_yoksa_handler_calismali()
    {
        var checker = Substitute.For<IPermissionChecker>();
        var behavior = new AuthorizationBehavior<UnprotectedRequest, Result<string>>(checker);

        var nextCalled = false;
        var response = await behavior.Handle(
            new UnprotectedRequest(),
            () => { nextCalled = true; return Task.FromResult(Result<string>.Success("ok")); },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
        checker.DidNotReceive().HasAnyPermission(Arg.Any<IReadOnlyList<string>>());
    }

    /// <summary>Permission var → handler çalışır.</summary>
    [Fact]
    public async Task Permission_varsa_handler_calismali()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasAnyPermission(Arg.Any<IReadOnlyList<string>>()).Returns(true);

        var behavior = new AuthorizationBehavior<ProtectedRequest, Result<string>>(checker);

        var response = await behavior.Handle(
            new ProtectedRequest(),
            () => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
    }

    /// <summary>Permission yok → AUTH-PERMISSION-DENIED + handler çalışmaz.</summary>
    [Fact]
    public async Task Permission_yoksa_403_donmeli_handler_calismamali()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasAnyPermission(Arg.Any<IReadOnlyList<string>>()).Returns(false);

        var behavior = new AuthorizationBehavior<ProtectedRequest, Result<string>>(checker);

        var nextCalled = false;
        var response = await behavior.Handle(
            new ProtectedRequest(),
            () => { nextCalled = true; return Task.FromResult(Result<string>.Success("ok")); },
            CancellationToken.None);

        nextCalled.Should().BeFalse();
        response.IsFailure.Should().BeTrue();
        response.FirstError.Code.Should().Be("AUTH-PERMISSION-DENIED");
        response.FirstError.Type.Should().Be(ErrorType.Forbidden);
    }

    public sealed record UnprotectedRequest : IRequest<Result<string>>;

    [RequirePermission("Test.Read")]
    public sealed record ProtectedRequest : IRequest<Result<string>>;
}
