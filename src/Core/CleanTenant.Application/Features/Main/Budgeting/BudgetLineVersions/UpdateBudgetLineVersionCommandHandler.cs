using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLineVersions;

/// <summary><see cref="UpdateBudgetLineVersionCommand"/> handler.</summary>
public sealed class UpdateBudgetLineVersionCommandHandler
    : IRequestHandler<UpdateBudgetLineVersionCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateBudgetLineVersionCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateBudgetLineVersionCommand request, CancellationToken cancellationToken)
    {
        var lineVersion = await _db.BudgetLineVersions
            .FirstOrDefaultAsync(x => x.Id == request.BudgetLineVersionId && !x.IsDeleted, cancellationToken);
        if (lineVersion is null)
            return Result.Failure(Error.NotFound("UBL-001", "Bütçe kalemi bulunamadı."));

        var version = await _db.BudgetVersions
            .FirstOrDefaultAsync(v => v.Id == lineVersion.BudgetVersionId && !v.IsDeleted, cancellationToken);
        if (version is null)
            return Result.Failure(Error.NotFound("UBL-001", "Bütçe versiyonu bulunamadı."));

        // Şirket sahipliği + Draft kontrolü
        var budgetOwned = await _db.Budgets.AnyAsync(
            b => b.Id == version.BudgetId && b.CompanyId == request.CompanyId && !b.IsDeleted, cancellationToken);
        if (!budgetOwned)
            return Result.Failure(Error.NotFound("UBL-001", "Bütçe kalemi bulunamadı."));

        if (version.PublishedAt is not null)
            return Result.Failure(Error.Failure("UBL-002", "Yayınlanmış versiyonun kalemi düzenlenemez; bütçeyi revize edin."));

        if (request.ParticipationGroupId is { } groupId)
        {
            var groupValid = await _db.ParticipationGroups.AnyAsync(
                g => g.Id == groupId && g.CompanyId == request.CompanyId && g.IsActive && !g.IsDeleted, cancellationToken);
            if (!groupValid)
                return Result.Failure(Error.Failure("UBL-003", "Geçersiz/pasif katılım grubu."));
        }

        lineVersion.PlannedAmount = request.PlannedAmount;
        lineVersion.PaymentSchedule = request.PaymentSchedule;
        lineVersion.DistributionModel = request.DistributionModel;
        lineVersion.ParticipationGroupId = request.ParticipationGroupId;
        lineVersion.DistributionConfig = request.DistributionConfig;
        lineVersion.DueDayOfMonth = request.DueDayOfMonth;
        lineVersion.InstallmentStartYear = request.InstallmentStartYear;
        lineVersion.InstallmentStartMonth = request.InstallmentStartMonth;
        lineVersion.InstallmentEndYear = request.InstallmentEndYear;
        lineVersion.InstallmentEndMonth = request.InstallmentEndMonth;
        lineVersion.InstallmentIntervalMonths = request.InstallmentIntervalMonths;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
