using CleanTenant.Domain.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using CleanTenant.Infrastructure.IntegrationTests.Fixtures;
using CleanTenant.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CleanTenant.Infrastructure.IntegrationTests.Catalog;

/// <summary>
/// FAZ B — Bütçe şablonu (Catalog) DB-level kısıt testleri:
/// (BudgetTemplateId, LineCode) benzersiz.
/// </summary>
public sealed class BudgetTemplateConstraintsTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public BudgetTemplateConstraintsTests(PostgresFixture fixture) => _fixture = fixture;

    private static BudgetTemplate NewTemplate(Guid? ownerTenantId = null) => new()
    {
        OwnerTenantId = ownerTenantId,
        Visibility = TemplateVisibility.Public,
        Type = BudgetType.Aidat,
        Name = $"Sablon-{Guid.NewGuid():N}",
    };

    private static BudgetTemplateLine NewLine(Guid templateId, string lineCode) => new()
    {
        BudgetTemplateId = templateId,
        CategoryCode = "GEN",
        CategoryName = "Genel",
        LineCode = lineCode,
        LineName = "Aidat",
        PaymentSchedule = PaymentSchedule.MonthlyEqual,
        DistributionModel = DistributionModel.Equal,
        DueDayOfMonth = 15,
        DisplayOrder = 0,
    };

    [Fact]
    public async Task Ayni_Template_LineCode_cifti_unique_ihlali()
    {
        var template = NewTemplate(Guid.NewGuid());

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            template.Lines.Add(NewLine(template.Id, "AID-01"));
            db.BudgetTemplates.Add(template);
            await db.SaveChangesAsync();
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            db.BudgetTemplateLines.Add(NewLine(template.Id, "AID-01")); // aynı çift

            var act = async () => await db.SaveChangesAsync();
            var assertion = await act.Should().ThrowAsync<DbUpdateException>();
            assertion.Which.InnerException.Should().BeOfType<PostgresException>()
                .Which.SqlState.Should().Be("23505");
        }
    }

    [Fact]
    public async Task UrlCode_otomatik_uretilir_ve_benzersiz()
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var t1 = NewTemplate(Guid.NewGuid());
        var t2 = NewTemplate(Guid.NewGuid());
        db.BudgetTemplates.Add(t1);
        db.BudgetTemplates.Add(t2);
        await db.SaveChangesAsync();

        t1.UrlCode.Should().HaveLength(9);
        t2.UrlCode.Should().HaveLength(9);
        t1.UrlCode.Should().NotBe(t2.UrlCode);
    }
}
