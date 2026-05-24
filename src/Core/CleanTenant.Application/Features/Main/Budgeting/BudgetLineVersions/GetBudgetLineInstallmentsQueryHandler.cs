using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="GetBudgetLineInstallmentsQuery"/> handler.</summary>
public sealed class GetBudgetLineInstallmentsQueryHandler
    : IRequestHandler<GetBudgetLineInstallmentsQuery, Result<IReadOnlyList<BudgetLineInstallmentDto>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetBudgetLineInstallmentsQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BudgetLineInstallmentDto>>> Handle(
        GetBudgetLineInstallmentsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await (
            from inst in _db.BudgetLineInstallments
            join lv in _db.BudgetLineVersions on inst.BudgetLineVersionId equals lv.Id
            join v in _db.BudgetVersions on lv.BudgetVersionId equals v.Id
            join b in _db.Budgets on v.BudgetId equals b.Id
            where inst.BudgetLineVersionId == request.BudgetLineVersionId
                && b.CompanyId == request.CompanyId
                && !inst.IsDeleted && !lv.IsDeleted && !v.IsDeleted && !b.IsDeleted
            orderby inst.Year, inst.Month
            select new BudgetLineInstallmentDto(
                inst.Id,
                inst.InstallmentNumber,
                inst.Year,
                inst.Month,
                inst.Amount,
                inst.Label,
                inst.IsManuallyEdited)
        ).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BudgetLineInstallmentDto>>.Success(items.AsReadOnly());
    }
}
