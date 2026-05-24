using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Budgeting;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>
/// <para>
/// Bir bütçenin tasarımını tenant'lar-arası paylaşılabilir <see cref="BudgetTemplate"/>
/// olarak kaydeder (Catalog'a). Kaynak tasarım: bütçe yayınlıysa aktif versiyon,
/// değilse taslak versiyon. <b>Yapı-only:</b> tutarlar taşınmaz; yalnız kalem
/// yapısı (kategori/kalem/grup kod+ad + ödeme planı + dağıtım modeli) denormalize edilir.
/// </para>
/// </summary>
[RequirePermission("tenant.budget.template.publish")]
public sealed record SaveBudgetAsTemplateCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid SourceBudgetId,
    string TemplateName,
    string? Description,
    TemplateVisibility Visibility) : IRequest<Result<Guid>>;
