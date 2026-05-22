using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <para>
/// Yeni taslak bütçe oluşturur. (CompanyId, FiscalYearId, Type, Title) çifti
/// benzersizdir; aynı yıl + tip + isimle ikinci kez çağrılırsa BDG-001 döner.
/// Aynı yıl + tipte farklı isimle birden fazla bütçe açılabilir (ek aidat,
/// çoklu yatırım).
/// </para>
/// <para>
/// Komut, Budget aggregate'ini Draft durumunda yaratır ve ona bağlı boş bir
/// taslak <c>BudgetVersion</c> (VersionNumber=1, PublishedAt=null) ekler.
/// </para>
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateBudgetCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid FiscalYearId,
    BudgetType Type,
    string Title,
    string? Notes = null,
    // Bütçe geçerlilik dönemi — verilmezse handler FiscalYear aralığından doldurur.
    int? PeriodStartYear = null,
    int? PeriodStartMonth = null,
    int? PeriodEndYear = null,
    int? PeriodEndMonth = null) : IRequest<Result<Guid>>;
