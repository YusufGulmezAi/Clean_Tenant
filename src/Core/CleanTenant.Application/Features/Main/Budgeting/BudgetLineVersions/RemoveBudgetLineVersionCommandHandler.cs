using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="RemoveBudgetLineVersionCommand"/> handler.</summary>
public sealed class RemoveBudgetLineVersionCommandHandler
    : IRequestHandler<RemoveBudgetLineVersionCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveBudgetLineVersionCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveBudgetLineVersionCommand request, CancellationToken cancellationToken)
    {
        var lineVersion = await _db.BudgetLineVersions
            .FirstOrDefaultAsync(x => x.Id == request.BudgetLineVersionId && !x.IsDeleted, cancellationToken);
        if (lineVersion is null)
            return Result.Failure(Error.NotFound("RBL-001", "Bütçe kalemi bulunamadı."));

        var version = await _db.BudgetVersions
            .FirstOrDefaultAsync(v => v.Id == lineVersion.BudgetVersionId && !v.IsDeleted, cancellationToken);
        if (version is null)
            return Result.Failure(Error.NotFound("RBL-001", "Bütçe versiyonu bulunamadı."));

        var budgetOwned = await _db.Budgets.AnyAsync(
            b => b.Id == version.BudgetId && b.CompanyId == request.CompanyId && !b.IsDeleted, cancellationToken);
        if (!budgetOwned)
            return Result.Failure(Error.NotFound("RBL-001", "Bütçe kalemi bulunamadı."));

        if (version.PublishedAt is not null)
            return Result.Failure(Error.Failure("RBL-002", "Yayınlanmış versiyondan kalem kaldırılamaz; bütçeyi revize edin."));

        lineVersion.IsDeleted = true;

        var installments = await _db.BudgetLineInstallments
            .Where(i => i.BudgetLineVersionId == lineVersion.Id && !i.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var inst in installments)
            inst.IsDeleted = true;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
