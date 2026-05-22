using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <para>
/// Yeni taslak bütçe oluşturur. (CompanyId, FiscalYearId) çifti benzersizdir;
/// aynı mali yıl için ikinci kez çağrılırsa BDG-001 hatası döner.
/// </para>
/// <para>
/// Komut, Budget aggregate'ini Draft durumunda yaratır ve ona bağlı boş bir
/// taslak <c>BudgetVersion</c> (VersionNumber=1, PublishedAt=null) eklemez —
/// bu versiyon ilk kalem eklendiğinde veya PublishBudget'ta oluşturulur (basit MVP).
/// </para>
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CreateBudgetCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid FiscalYearId,
    string Title,
    string? Notes = null) : IRequest<Result<Guid>>;
