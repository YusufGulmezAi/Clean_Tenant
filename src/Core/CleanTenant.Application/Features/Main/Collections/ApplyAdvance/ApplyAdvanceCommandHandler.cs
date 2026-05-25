using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accruals.Enums;
using CleanTenant.Domain.Tenant.Collections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Collections.ApplyAdvance;

/// <summary>
/// <see cref="ApplyAdvanceCommand"/> handler — avansları açık borçlara FIFO mahsup eder
/// (GL-nötr; yeni <c>CollectionAllocation</c> + <c>UnallocatedAmount</c> düşümü).
/// </summary>
public sealed class ApplyAdvanceCommandHandler
    : IRequestHandler<ApplyAdvanceCommand, Result<AdvanceApplicationResult>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ApplyAdvanceCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AdvanceApplicationResult>> Handle(
        ApplyAdvanceCommand request, CancellationToken cancellationToken)
    {
        // Avans kaynakları: bu BB'nin dağıtılmamış bakiyesi olan tahsilatları (en eski önce)
        var advances = await _db.Collections
            .Where(c => c.UnitId == request.UnitId && c.CompanyId == request.CompanyId
                     && !c.IsDeleted && c.UnallocatedAmount > 0m)
            .OrderBy(c => c.PaymentDate).ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        var totalAdvance = advances.Sum(c => c.UnallocatedAmount);
        if (totalAdvance <= 0m)
            return Result<AdvanceApplicationResult>.Success(new AdvanceApplicationResult(0m, 0, 0m));

        // Açık tahakkuk detayları (TBK m.101: en eski vade; önce gecikme, sonra anapara)
        var details = await (
            from d in _db.AccrualDetails
            join a in _db.Accruals on d.AccrualId equals a.Id
            where d.UnitId == request.UnitId && a.CompanyId == request.CompanyId
                && !d.IsDeleted && !a.IsDeleted && a.Source != AccrualSource.Correction
            select new { d.Id, d.Amount, d.DueDate, a.Source }
        ).ToListAsync(cancellationToken);

        var detailIds = details.Select(x => x.Id).ToList();
        var allocatedMap = (await _db.CollectionAllocations
            .Where(al => detailIds.Contains(al.AccrualDetailId) && !al.IsDeleted)
            .GroupBy(al => al.AccrualDetailId)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => x.AllocatedAmount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        // Ters kayıt netlemesi: orijinal detaya bağlı Correction tutarları açık borçtan düşülür
        var correctionMap = (await _db.AccrualDetails
            .Where(d => d.CorrectedAccrualDetailId != null
                     && detailIds.Contains(d.CorrectedAccrualDetailId.Value) && !d.IsDeleted)
            .GroupBy(d => d.CorrectedAccrualDetailId!.Value)
            .Select(g => new { DetailId = g.Key, Sum = g.Sum(x => -x.Amount) })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.DetailId, x => x.Sum);

        var open = details
            .Select(d => new
            {
                d.Id,
                d.Source,
                d.DueDate,
                Remaining = d.Amount - allocatedMap.GetValueOrDefault(d.Id, 0m)
                            - correctionMap.GetValueOrDefault(d.Id, 0m),
            })
            .Where(d => d.Remaining > 0m)
            .OrderBy(d => d.DueDate)
            .ThenByDescending(d => d.Source == AccrualSource.LateFee)
            .ThenBy(d => d.Id)
            .ToList();

        if (open.Count == 0)
            return Result<AdvanceApplicationResult>.Success(
                new AdvanceApplicationResult(0m, 0, totalAdvance));

        // İki-işaretçi mahsup: avanslar (eski→yeni) açık borçlara (FIFO) uygulanır.
        var applied = 0m;
        var allocationCount = 0;
        var ai = 0;
        foreach (var d in open)
        {
            var debtRemaining = d.Remaining;
            while (debtRemaining > 0m && ai < advances.Count)
            {
                var adv = advances[ai];
                if (adv.UnallocatedAmount <= 0m) { ai++; continue; }

                var apply = Math.Min(debtRemaining, adv.UnallocatedAmount);
                _db.CollectionAllocations.Add(new CollectionAllocation
                {
                    TenantId = request.TenantId,
                    CollectionId = adv.Id,
                    AccrualDetailId = d.Id,
                    AllocatedAmount = apply,
                });
                adv.UnallocatedAmount -= apply;
                debtRemaining -= apply;
                applied += apply;
                allocationCount++;
            }
            if (ai >= advances.Count) break;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AdvanceApplicationResult>.Success(
            new AdvanceApplicationResult(applied, allocationCount, totalAdvance - applied));
    }
}
