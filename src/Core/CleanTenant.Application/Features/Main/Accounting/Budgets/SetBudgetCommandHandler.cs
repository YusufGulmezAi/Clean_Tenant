using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Budgets;

/// <summary>
/// <see cref="SetBudgetCommand"/> handler — upsert mantığı ile çalışır.
/// </summary>
public sealed class SetBudgetCommandHandler
    : IRequestHandler<SetBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(
        SetBudgetCommand command,
        CancellationToken cancellationToken)
    {
        // Hesap kodu yaprak mı kontrolü (ACC-601)
        var accountCode = await _db.AccountCodes
            .FirstOrDefaultAsync(ac => ac.Id == command.AccountCodeId
                                    && ac.CompanyId == command.CompanyId
                                    && !ac.IsDeleted, cancellationToken);

        if (accountCode is null)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-001", "Hesap kodu bulunamadı."));

        if (!accountCode.IsDetail)
            return Result<Guid>.Failure(
                Error.Failure("ACC-601", "Yalnızca yaprak hesaplara bütçe girilebilir."));

        // Muhasebe dönemi kontrolü
        var periodExists = await _db.AccountingPeriods
            .AnyAsync(p => p.Id == command.AccountingPeriodId
                        && p.CompanyId == command.CompanyId
                        && !p.IsDeleted, cancellationToken);

        if (!periodExists)
            return Result<Guid>.Failure(
                Error.NotFound("ACC-004", "Muhasebe dönemi bulunamadı."));

        // Upsert: mevcut bütçe kalemini ara
        var existing = await _db.BudgetEntries
            .FirstOrDefaultAsync(b => b.CompanyId == command.CompanyId
                                   && b.AccountingPeriodId == command.AccountingPeriodId
                                   && b.AccountCodeId == command.AccountCodeId
                                   && b.CostCenterId == command.CostCenterId
                                   && !b.IsDeleted, cancellationToken);

        Guid budgetId;

        if (existing is not null)
        {
            // Güncelle
            existing.BudgetedAmount = command.BudgetedAmount;
            budgetId = existing.Id;
        }
        else
        {
            // Yeni kayıt
            var budget = new BudgetEntry
            {
                TenantId = command.TenantId,
                CompanyId = command.CompanyId,
                AccountingPeriodId = command.AccountingPeriodId,
                AccountCodeId = command.AccountCodeId,
                CostCenterId = command.CostCenterId,
                BudgetedAmount = command.BudgetedAmount
            };
            _db.BudgetEntries.Add(budget);
            budgetId = budget.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(budgetId);
    }
}
