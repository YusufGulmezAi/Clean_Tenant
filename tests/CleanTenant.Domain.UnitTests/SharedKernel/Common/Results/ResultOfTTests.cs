using CleanTenant.SharedKernel.Common.Errors;
using CleanTenant.SharedKernel.Common.Results;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Common.Results;

public sealed class ResultOfTTests
{
    [Fact]
    public void Success_value_ile_basarili_uretir()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_value_default_doner()
    {
        var result = Result<int>.Failure(Error.NotFound("USR-404", "yok"));

        result.IsFailure.Should().BeTrue();
        result.Value.Should().Be(default);
    }

    [Fact]
    public void Implicit_value_dan_Success_a_donusur()
    {
        Result<string> result = "merhaba";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("merhaba");
    }

    [Fact]
    public void Implicit_Error_dan_Failure_a_donusur()
    {
        var error = Error.Forbidden("AUT-403", "yetki yok");

        Result<string> result = error;

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().Be(error);
    }

    [Fact]
    public void Map_basarili_sonucta_donusum_uygular()
    {
        var result = Result<int>.Success(5);

        var mapped = result.Map(x => x * 2);

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_basarisiz_sonucta_hata_zincirleyerek_tasir()
    {
        var error = Error.Validation("VAL-001", "Sıfır olamaz");
        var result = Result<int>.Failure(error);

        var mapped = result.Map(x => x * 2);

        mapped.IsFailure.Should().BeTrue();
        mapped.FirstError.Should().Be(error);
    }

    [Fact]
    public void Bind_basarili_sonucta_zincirlemeli_Result_uretir()
    {
        var result = Result<int>.Success(10);

        var bound = result.Bind(x => Result<string>.Success($"sayi:{x}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("sayi:10");
    }

    [Fact]
    public void Match_basarida_onSuccess_basarisizda_onFailure_calistirir()
    {
        var success = Result<int>.Success(7);
        var failure = Result<int>.Failure(Error.Failure("INV", "iptal"));

        success.Match(v => $"ok:{v}", _ => "fail").Should().Be("ok:7");
        failure.Match(_ => "ok", errs => $"fail:{errs[0].Code}").Should().Be("fail:INV");
    }
}
