using CleanTenant.Domain.Tenant.Companies;
using CleanTenant.SharedKernel.Entities;

namespace CleanTenant.Domain.UnitTests.Tenant.Companies;

/// <summary>
/// <para>
/// Company entity marker + default state davranışı için unit testler.
/// Company POCO setter'lı bir entity (Faz 1'in ilk iş varlığı); davranış
/// invariant'ları DB CHECK constraint'leri ve FluentValidation handler'larıyla
/// (v0.2.3.b) uygulanır. Bu testler regresyon guard'ı:
/// </para>
/// <list type="bullet">
///   <item>ITenantScoped, IHasUrlCode, IAggregateRoot marker'larının kazara
///   kaldırılmasını yakalar — silinirse global query filter ve audit zinciri kırılır.</item>
///   <item>Default Name/UrlCode değerlerinin null değil empty string olduğunu garanti eder.</item>
///   <item>CompanyStatus enum sıralaması <c>Active=1</c>, <c>Suspended=2</c>, <c>Closed=3</c>
///   olarak DB'ye yazılıdır; kazara renumbering veri uyumsuzluğu doğurur.</item>
/// </list>
/// </summary>
public sealed class CompanyTests
{
    [Fact]
    public void Default_constructor_yields_empty_strings_and_default_status()
    {
        var company = new Company();

        company.Name.Should().BeEmpty();
        company.UrlCode.Should().BeEmpty();
        company.LegalName.Should().BeNull();
        company.Vkn.Should().BeNull();
        company.Email.Should().BeNull();
        company.Phone.Should().BeNull();
        company.TenantId.Should().Be(Guid.Empty);
        company.Status.Should().Be(default(CompanyStatus));
    }

    [Fact]
    public void Implements_required_marker_interfaces()
    {
        typeof(ITenantScoped).IsAssignableFrom(typeof(Company)).Should().BeTrue(
            "global query filter ITenantScoped üzerinden TenantId filtresi uygular");
        typeof(IAggregateRoot).IsAssignableFrom(typeof(Company)).Should().BeTrue(
            "audit + repository sınırı IAggregateRoot ile belirlenir");
        typeof(IHasUrlCode).IsAssignableFrom(typeof(Company)).Should().BeTrue(
            "UrlCodeGeneratingInterceptor IHasUrlCode entity'lerine 9-char Base58 atar");
        typeof(BaseEntity).IsAssignableFrom(typeof(Company)).Should().BeTrue(
            "Id + audit + soft-delete kolonları BaseEntity'den gelir");
    }

    [Theory]
    [InlineData(CompanyStatus.Active, 1)]
    [InlineData(CompanyStatus.Suspended, 2)]
    [InlineData(CompanyStatus.Closed, 3)]
    public void CompanyStatus_enum_values_remain_stable(CompanyStatus status, int expectedNumeric)
    {
        // DB'de int olarak saklandığı için kazara yeniden numaralandırma
        // mevcut tenant verilerini yanlış status'e map eder. Bu test
        // sıralamanın değişmemesini garanti eder.
        ((int)status).Should().Be(expectedNumeric);
    }
}
