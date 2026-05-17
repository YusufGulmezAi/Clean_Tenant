using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Common.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_basarili_ve_hata_listesi_bos()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.FirstError.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_tek_hata_ile_basarisiz_uretir()
    {
        var error = Error.Validation("VAL-001", "Zorunlu alan");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Be(error);
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void Failure_birden_cok_hata_ile_basarisiz_uretir()
    {
        var errors = new[]
        {
            Error.Validation("VAL-001", "A"),
            Error.Validation("VAL-002", "B"),
        };

        var result = Result.Failure(errors);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.FirstError.Should().Be(errors[0]);
    }

    [Fact]
    public void Success_durumunda_FirstError_None_doner()
    {
        var result = Result.Success();

        result.FirstError.Should().Be(Error.None);
    }
}
