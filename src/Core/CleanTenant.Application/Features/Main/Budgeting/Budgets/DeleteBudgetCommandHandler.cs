using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary><see cref="DeleteBudgetCommand"/> handler.</summary>
public sealed class DeleteBudgetCommandHandler : IRequestHandler<DeleteBudgetCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeleteBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.BudgetId
                                   && b.CompanyId == request.CompanyId
                                   && !b.IsDeleted, cancellationToken);
        if (budget is null)
            return Result.Failure(Error.NotFound("DBG-001", "Bütçe bulunamadı."));
        if (budget.Status != BudgetStatus.Draft)
            return Result.Failure(Error.Failure("DBG-002", "Yalnız taslak bütçe silinebilir."));

        var versions = await _db.BudgetVersions
            .Where(v => v.BudgetId == budget.Id && !v.IsDeleted)
            .ToListAsync(cancellationToken);
        var versionIds = versions.Select(v => v.Id).ToList();

        var lineVersions = await _db.BudgetLineVersions
            .Where(lv => versionIds.Contains(lv.BudgetVersionId) && !lv.IsDeleted)
            .ToListAsync(cancellationToken);
        var lineVersionIds = lineVersions.Select(lv => lv.Id).ToList();

        var installments = await _db.BudgetLineInstallments
            .Where(i => lineVersionIds.Contains(i.BudgetLineVersionId) && !i.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var inst in installments) inst.IsDeleted = true;
        foreach (var lv in lineVersions) lv.IsDeleted = true;
        foreach (var v in versions) v.IsDeleted = true;
        budget.IsDeleted = true;
        budget.CurrentVersionId = null;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
