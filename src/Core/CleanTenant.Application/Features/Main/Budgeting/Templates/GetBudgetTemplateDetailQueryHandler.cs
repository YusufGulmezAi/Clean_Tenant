using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Budgeting;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary><see cref="GetBudgetTemplateDetailQuery"/> handler.</summary>
public sealed class GetBudgetTemplateDetailQueryHandler
    : IRequestHandler<GetBudgetTemplateDetailQuery, Result<BudgetTemplateDetail>>
{
    private readonly ICatalogDbContext _catalog;
    private readonly ITenantContext _tenant;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetTemplateDetailQueryHandler(ICatalogDbContext catalog, ITenantContext tenant)
    {
        _catalog = catalog;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public async Task<Result<BudgetTemplateDetail>> Handle(
        GetBudgetTemplateDetailQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;

        var template = await _catalog.BudgetTemplates
            .Where(t => t.Id == request.TemplateId && !t.IsDeleted
                && (t.Visibility == TemplateVisibility.Public
                    || t.OwnerTenantId == null
                    || t.OwnerTenantId == tenantId))
            .Select(t => new BudgetTemplateDetail(
                t.Id,
                t.UrlCode,
                t.OwnerTenantId,
                t.Visibility,
                t.Type,
                t.Name,
                t.Description,
                t.Lines
                    .Where(l => !l.IsDeleted)
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => new BudgetTemplateLineItem(
                        l.CategoryCode,
                        l.CategoryName,
                        l.LineCode,
                        l.LineName,
                        l.PaymentSchedule,
                        l.DistributionModel,
                        l.DueDayOfMonth,
                        l.ParticipationGroupName,
                        l.InstallmentIntervalMonths,
                        l.InstallmentCount))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
            return Result<BudgetTemplateDetail>.Failure(Error.NotFound("STP-010", "Bütçe şablonu bulunamadı."));

        return Result<BudgetTemplateDetail>.Success(template);
    }
}
