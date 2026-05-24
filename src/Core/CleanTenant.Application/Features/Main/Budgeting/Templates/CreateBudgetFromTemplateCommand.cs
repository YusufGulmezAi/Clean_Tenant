using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>
/// <para>
/// Bir bütçe şablonundan hedef şirkette yeni <c>Draft</c> bütçe (V1) oluşturur.
/// Kategori/kalem/katılım grubu hedef şirkette <b>kod'a göre bul-veya-oluştur</b>
/// ile eşlenir. Şablon yapı-only olduğundan kalem tutarları <b>0</b> gelir; site
/// tutarları girip yayınlar.
/// </para>
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateBudgetFromTemplateCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid TemplateId,
    Guid FiscalYearId,
    string Title,
    int? PeriodStartYear = null,
    int? PeriodStartMonth = null,
    int? PeriodEndYear = null,
    int? PeriodEndMonth = null) : IRequest<Result<Guid>>;
