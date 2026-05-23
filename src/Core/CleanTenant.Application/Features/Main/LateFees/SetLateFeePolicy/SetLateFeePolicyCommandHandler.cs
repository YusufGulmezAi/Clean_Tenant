using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.LateFees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.LateFees.SetLateFeePolicy;

/// <summary><see cref="SetLateFeePolicyCommand"/> handler — upsert + doğrulama.</summary>
public sealed class SetLateFeePolicyCommandHandler
    : IRequestHandler<SetLateFeePolicyCommand, Result<LateFeePolicyResult>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SetLateFeePolicyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<LateFeePolicyResult>> Handle(
        SetLateFeePolicyCommand request, CancellationToken cancellationToken)
    {
        // KMK m.20 tavan kontrolü (validator da kontrol eder; defansif)
        if (request.MonthlyRatePercent <= 0m
            || request.MonthlyRatePercent > RegulatoryLimits.KmkM20MonthlyCapPercent)
            return Result<LateFeePolicyResult>.Failure(Error.Failure(
                "LFP-001",
                $"Oran 0 ile KMK m.20 tavanı (%{RegulatoryLimits.KmkM20MonthlyCapPercent:0.##}) arasında olmalı."));

        // Gelir hesabı geçerli mi (yaprak, aktif, şirkete ait)
        var incomeValid = await _db.AccountCodes.AnyAsync(
            a => a.Id == request.IncomeAccountCodeId
                && a.CompanyId == request.CompanyId
                && a.IsActive && a.IsDetail && !a.IsDeleted,
            cancellationToken);
        if (!incomeValid)
            return Result<LateFeePolicyResult>.Failure(Error.Failure(
                "LFP-002", "Geçersiz/pasif/özet gelir hesabı (yaprak gerekir)."));

        // Bütçe override ise bütçe geçerli mi
        if (request.BudgetId is { } budgetId)
        {
            var budgetExists = await _db.Budgets.AnyAsync(
                b => b.Id == budgetId && b.CompanyId == request.CompanyId && !b.IsDeleted,
                cancellationToken);
            if (!budgetExists)
                return Result<LateFeePolicyResult>.Failure(Error.NotFound(
                    "LFP-003", "Bütçe bulunamadı."));
        }

        // Upsert: aynı kapsamdaki (Company + BudgetId) mevcut politika
        var query = _db.LateFeePolicies.Where(p => p.CompanyId == request.CompanyId && !p.IsDeleted);
        query = request.BudgetId is { } bid
            ? query.Where(p => p.BudgetId == bid)
            : query.Where(p => p.BudgetId == null);
        var existing = await query.FirstOrDefaultAsync(cancellationToken);

        bool created;
        LateFeePolicy policy;
        if (existing is null)
        {
            policy = new LateFeePolicy
            {
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                BudgetId = request.BudgetId,
                MonthlyRatePercent = request.MonthlyRatePercent,
                IsCompound = request.IsCompound,
                GraceDays = request.GraceDays,
                IncomeAccountCodeId = request.IncomeAccountCodeId,
                IsActive = true,
            };
            _db.LateFeePolicies.Add(policy);
            created = true;
        }
        else
        {
            existing.MonthlyRatePercent = request.MonthlyRatePercent;
            existing.IsCompound = request.IsCompound;
            existing.GraceDays = request.GraceDays;
            existing.IncomeAccountCodeId = request.IncomeAccountCodeId;
            existing.IsActive = true;
            policy = existing;
            created = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<LateFeePolicyResult>.Success(new LateFeePolicyResult(policy.Id, created));
    }
}
