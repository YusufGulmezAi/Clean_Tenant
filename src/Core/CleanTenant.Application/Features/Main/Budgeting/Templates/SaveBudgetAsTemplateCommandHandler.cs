using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Templates;

/// <summary>
/// <see cref="SaveBudgetAsTemplateCommand"/> handler — Main'den kaynak bütçe tasarımını
/// okur, Catalog'a yapı-only şablon olarak denormalize eder.
/// </summary>
public sealed class SaveBudgetAsTemplateCommandHandler
    : IRequestHandler<SaveBudgetAsTemplateCommand, Result<Guid>>
{
    private readonly IMainDbContext _main;
    private readonly ICatalogDbContext _catalog;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SaveBudgetAsTemplateCommandHandler(IMainDbContext main, ICatalogDbContext catalog)
    {
        _main = main;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(SaveBudgetAsTemplateCommand request, CancellationToken cancellationToken)
    {
        var budget = await _main.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.SourceBudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);
        if (budget is null)
            return Result<Guid>.Failure(Error.NotFound("STP-001", "Kaynak bütçe bulunamadı."));

        // Tasarım versiyonu: aktif yayınlı veya tek taslak
        Guid versionId;
        if (budget.CurrentVersionId is { } current)
        {
            versionId = current;
        }
        else
        {
            var draft = await _main.BudgetVersions
                .Where(v => v.BudgetId == budget.Id && !v.IsDeleted)
                .OrderBy(v => v.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
            if (draft is null)
                return Result<Guid>.Failure(Error.Failure("STP-002", "Kaynak bütçede versiyon bulunamadı."));
            versionId = draft.Id;
        }

        var lineVersions = await _main.BudgetLineVersions
            .Where(lv => lv.BudgetVersionId == versionId && !lv.IsDeleted)
            .ToListAsync(cancellationToken);
        if (lineVersions.Count == 0)
            return Result<Guid>.Failure(Error.Failure("STP-003", "Kaynak versiyonda kalem yok; şablon boş olamaz."));

        var lineIds = lineVersions.Select(lv => lv.BudgetLineId).Distinct().ToList();
        var lines = await _main.BudgetLines
            .Where(l => lineIds.Contains(l.Id) && !l.IsDeleted)
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        // Kategoriler (şirket geneli — parent kod çözümü için)
        var categories = await _main.ExpenseCategories
            .Where(c => c.CompanyId == request.CompanyId && !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var groupIds = lineVersions
            .Where(lv => lv.ParticipationGroupId.HasValue)
            .Select(lv => lv.ParticipationGroupId!.Value)
            .Distinct()
            .ToList();
        var groups = await _main.ParticipationGroups
            .Where(g => groupIds.Contains(g.Id) && !g.IsDeleted)
            .ToDictionaryAsync(g => g.Id, cancellationToken);

        // Taksit adedi (kalem versiyonu başına)
        var lvIds = lineVersions.Select(lv => lv.Id).ToList();
        var installmentCounts = (await _main.BudgetLineInstallments
            .Where(i => lvIds.Contains(i.BudgetLineVersionId) && !i.IsDeleted)
            .GroupBy(i => i.BudgetLineVersionId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.Id, x => x.Count);

        var template = new BudgetTemplate
        {
            OwnerTenantId = request.TenantId,
            Visibility = request.Visibility,
            Type = budget.Type,
            Name = request.TemplateName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        };

        var order = 0;
        foreach (var lv in lineVersions)
        {
            if (!lines.TryGetValue(lv.BudgetLineId, out var line))
                continue;

            categories.TryGetValue(line.ExpenseCategoryId, out var category);
            string? parentCode = null;
            if (category?.ParentCategoryId is { } parentId
                && categories.TryGetValue(parentId, out var parent))
                parentCode = parent.Code;

            string? groupCode = null, groupName = null;
            if (lv.ParticipationGroupId is { } gid && groups.TryGetValue(gid, out var group))
            {
                groupCode = group.Code;
                groupName = group.Name;
            }

            var installmentCount = installmentCounts.GetValueOrDefault(lv.Id);

            template.Lines.Add(new BudgetTemplateLine
            {
                CategoryCode = category?.Code ?? "GEN",
                CategoryName = category?.Name ?? "Genel",
                ParentCategoryCode = parentCode,
                LineCode = line.Code,
                LineName = line.Name,
                LineDescription = line.Description,
                PaymentSchedule = lv.PaymentSchedule,
                DistributionModel = lv.DistributionModel,
                DistributionConfig = lv.DistributionConfig,
                DueDayOfMonth = lv.DueDayOfMonth,
                ParticipationGroupCode = groupCode,
                ParticipationGroupName = groupName,
                InstallmentIntervalMonths = lv.InstallmentIntervalMonths,
                InstallmentCount = installmentCount > 0 ? installmentCount : null,
                DisplayOrder = order++,
            });
        }

        _catalog.BudgetTemplates.Add(template);
        await _catalog.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(template.Id);
    }
}
