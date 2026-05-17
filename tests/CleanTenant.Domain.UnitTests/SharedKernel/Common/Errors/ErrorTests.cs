using CleanTenant.SharedKernel.Common.Errors;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Common.Errors;

public sealed class ErrorTests
{
    [Fact]
    public void Validation_factory_Validation_tipinde_Error_uretir()
    {
        var error = Error.Validation("VAL-001", "Alan zorunlu");

        error.Code.Should().Be("VAL-001");
        error.Message.Should().Be("Alan zorunlu");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_factory_NotFound_tipinde_Error_uretir()
    {
        var error = Error.NotFound("USR-404", "Kullanıcı bulunamadı");

        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_factory_Conflict_tipinde_Error_uretir()
    {
        var error = Error.Conflict("USR-409", "E-posta zaten kayıtlı");

        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_factory_Unauthorized_tipinde_Error_uretir()
    {
        var error = Error.Unauthorized("AUT-401", "Oturum yok");

        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_factory_Forbidden_tipinde_Error_uretir()
    {
        var error = Error.Forbidden("AUT-403", "Yetki yok");

        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Failure_factory_Failure_tipinde_Error_uretir()
    {
        var error = Error.Failure("INV-001", "Fatura kapalı");

        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Critical_factory_Critical_tipinde_Error_uretir()
    {
        var error = Error.Critical("GEN-500", "Beklenmedik");

        error.Type.Should().Be(ErrorType.Critical);
    }

    [Fact]
    public void None_static_alani_None_tipinde_bos_Error()
    {
        Error.None.Type.Should().Be(ErrorType.None);
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Iki_ayni_kod_mesaj_tip_Error_record_esitligi_saglar()
    {
        var a = Error.Validation("VAL-001", "Boş");
        var b = Error.Validation("VAL-001", "Boş");

        a.Should().Be(b);
    }
}
