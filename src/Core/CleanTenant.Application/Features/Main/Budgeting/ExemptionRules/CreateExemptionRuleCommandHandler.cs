using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ExemptionRules;

/// <summary><see cref="CreateExemptionRuleCommand"/> handler. BB + kalem doğrulama,
/// tarih sanity ve overlapping muafiyet uyarısı (warning değil — engelleyici BDG-703).</summary>
public sealed class CreateExemptionRuleCommandHandler
    : IRequestHandler<CreateExemptionRuleCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateExemptionRuleCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateExemptionRuleCommand request, CancellationToken cancellationToken)
    {
        // BB ve kalem doğrulama
        var unitOk = await _db.Units.AnyAsync(u => u.Id == request.UnitId && !u.IsDeleted, cancellationToken);
        if (!unitOk)
            return Result<Guid>.Failure(Error.NotFound("BDG-700", "Bağımsız Bölüm bulunamadı."));

        var lineOk = await _db.BudgetLines
            .AnyAsync(l => l.Id == request.BudgetLineId
                        && l.CompanyId == request.CompanyId
                        && !l.IsDeleted, cancellationToken);
        if (!lineOk)
            return Result<Guid>.Failure(Error.NotFound("BDG-701", "Bütçe kalemi bulunamadı."));

        // Tarih sanity
        if (request.ValidTo is { } to && to < request.ValidFrom)
            return Result<Guid>.Failure(
                Error.Failure("BDG-702", "Bitiş tarihi başlangıçtan önce olamaz."));

        // Çakışan aktif muafiyet kontrolü (BDG-703)
        // İki aralık çakışır: A.From <= B.To AND B.From <= A.To  (To=null → +∞)
        var overlapping = await _db.ExemptionRules
            .AnyAsync(e => e.UnitId == request.UnitId
                        && e.BudgetLineId == request.BudgetLineId
                        && !e.IsDeleted
                        && (request.ValidTo == null || e.ValidFrom <= request.ValidTo)
                        && (e.ValidTo == null || e.ValidTo >= request.ValidFrom),
                cancellationToken);
        if (overlapping)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-703", "Bu BB ve kalem için tarih aralığı çakışan muafiyet mevcut."));

        var rule = new ExemptionRule
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            UnitId = request.UnitId,
            BudgetLineId = request.BudgetLineId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Reason = request.Reason.Trim()
        };

        _db.ExemptionRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(rule.Id);
    }
}
