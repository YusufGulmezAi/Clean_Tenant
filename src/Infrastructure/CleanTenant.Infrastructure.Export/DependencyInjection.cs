using CleanTenant.Application.Common.Export;
using CleanTenant.Application.Features.Main.BuildingSchema.Excel;
using CleanTenant.Infrastructure.Export.BuildingSchema;
using CleanTenant.Infrastructure.Export.Excel;
using CleanTenant.Infrastructure.Export.Pdf;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace CleanTenant.Infrastructure.Export;

/// <summary>
/// <para>
/// Export servislerinin (Excel + PDF) DI kayıt extension'ı. v0.2.4.a.
/// </para>
/// <para>
/// <b>QuestPDF lisans:</b> boot anında <c>LicenseType.Community</c> set edilir.
/// 2023.12.x sürümü community license şartlarıyla ücretsiz kalır. CleanTenant
/// $1M+ gelir eşiğini aştığında commercial lisans gerekli.
/// </para>
/// </summary>
public static class DependencyInjection
{
    /// <summary>Export servislerini DI'a kayıt eder + QuestPDF lisansını set eder.</summary>
    public static IServiceCollection AddCleanTenantExport(this IServiceCollection services)
    {
        // QuestPDF community license — 2023.12.x için ücretsiz
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddSingleton<IExcelExportService, ClosedXmlExportService>();
        services.AddSingleton<IPdfExportService, QuestPdfExportService>();
        services.AddSingleton<IBuildingSchemaExcelService, BuildingSchemaExcelService>();

        return services;
    }
}
