using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Budgeting;
using CleanTenant.SharedKernel.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary><see cref="GetSharedBudgetTemplatesQuery"/> handler.</summary>
public sealed class GetSharedBudgetTemplatesQueryHandler
    : IRequestHandler<GetSharedBudgetTemplatesQuery, Result<IReadOnlyList<BudgetTemplateListItem>>>
{
    private readonly ICatalogDbContext _catalog;
    private readonly ITenantContext _tenant;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetSharedBudgetTemplatesQueryHandler(ICatalogDbContext catalog, ITenantContext tenant)
    {
        _catalog = catalog;
        _tenant = tenant;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetTemplateListItem>>> Handle(
        GetSharedBudgetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId;

        var query = _catalog.BudgetTemplates
            .Where(t => !t.IsDeleted
                && (t.Visibility == TemplateVisibility.Public
                    || t.OwnerTenantId == null
                    || t.OwnerTenantId == tenantId));

        if (request.Type is { } type)
            query = query.Where(t => t.Type == type);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new BudgetTemplateListItem(
                t.Id,
                t.UrlCode,
                t.OwnerTenantId,
                t.Visibility,
                t.Type,
                t.Name,
                t.Description,
                t.Lines.Count(l => !l.IsDeleted),
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetTemplateListItem>>.Success(items.AsReadOnly());
    }
}
