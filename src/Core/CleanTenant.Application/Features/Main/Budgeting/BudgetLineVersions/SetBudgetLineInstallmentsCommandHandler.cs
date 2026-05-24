using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="SetBudgetLineInstallmentsCommand"/> handler.</summary>
public sealed class SetBudgetLineInstallmentsCommandHandler
    : IRequestHandler<SetBudgetLineInstallmentsCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetBudgetLineInstallmentsCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(SetBudgetLineInstallmentsCommand request, CancellationToken cancellationToken)
    {
        var lineVersion = await _db.BudgetLineVersions
            .FirstOrDefaultAsync(x => x.Id == request.BudgetLineVersionId && !x.IsDeleted, cancellationToken);
        if (lineVersion is null)
            return Result.Failure(Error.NotFound("SBI-001", "Bütçe kalemi bulunamadı."));

        var version = await _db.BudgetVersions
            .FirstOrDefaultAsync(v => v.Id == lineVersion.BudgetVersionId && !v.IsDeleted, cancellationToken);
        if (version is null)
            return Result.Failure(Error.NotFound("SBI-001", "Bütçe versiyonu bulunamadı."));

        var budgetOwned = await _db.Budgets.AnyAsync(
            b => b.Id == version.BudgetId && b.CompanyId == request.CompanyId && !b.IsDeleted, cancellationToken);
        if (!budgetOwned)
            return Result.Failure(Error.NotFound("SBI-001", "Bütçe kalemi bulunamadı."));

        if (version.PublishedAt is not null)
            return Result.Failure(Error.Failure("SBI-002", "Yayınlanmış versiyonun taksitleri düzenlenemez."));

        // (Year, Month) benzersizliği — aynı ay iki kez verilemez
        var duplicateMonth = request.Installments
            .GroupBy(i => (i.Year, i.Month))
            .Any(g => g.Count() > 1);
        if (duplicateMonth)
            return Result.Failure(Error.Failure("SBI-003", "Aynı (yıl, ay) için birden fazla taksit verilemez."));

        // Eskileri soft-delete et
        var existing = await _db.BudgetLineInstallments
            .Where(i => i.BudgetLineVersionId == lineVersion.Id && !i.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var inst in existing)
            inst.IsDeleted = true;

        // Yenileri ay sırasına göre ekle
        var ordered = request.Installments
            .OrderBy(i => i.Year).ThenBy(i => i.Month)
            .ToList();
        var number = 1;
        foreach (var input in ordered)
        {
            _db.BudgetLineInstallments.Add(new BudgetLineInstallment
            {
                TenantId = request.TenantId,
                BudgetLineVersionId = lineVersion.Id,
                InstallmentNumber = number++,
                Year = input.Year,
                Month = input.Month,
                Amount = input.Amount,
                Label = string.IsNullOrWhiteSpace(input.Label) ? null : input.Label.Trim(),
                IsManuallyEdited = true,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
