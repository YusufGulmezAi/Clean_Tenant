using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using CleanTenant.Domain.Tenant.Budgeting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <see cref="CreateBudgetCommand"/> handler — Draft bütçe yaratır + boş bir
/// Draft <c>BudgetVersion</c> (V1) ekler (kalem versiyonları daha sonra eklenir).
/// </summary>
public sealed class CreateBudgetCommandHandler
    : IRequestHandler<CreateBudgetCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        // Mali yıl var mı + bu şirkete mi ait
        var fiscalYearExists = await _db.FiscalYears
            .AnyAsync(fy => fy.Id == request.FiscalYearId
                         && fy.CompanyId == request.CompanyId
                         && !fy.IsDeleted, cancellationToken);

        if (!fiscalYearExists)
            return Result<Guid>.Failure(Error.NotFound("BDG-002", "Mali yıl bulunamadı."));

        // (CompanyId, FiscalYearId) benzersizliği
        var duplicate = await _db.Budgets
            .AnyAsync(b => b.CompanyId == request.CompanyId
                        && b.FiscalYearId == request.FiscalYearId
                        && !b.IsDeleted, cancellationToken);

        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-001", "Bu mali yıl için bir bütçe zaten mevcut."));

        // Bütçe (Draft)
        var budget = new Budget
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            FiscalYearId = request.FiscalYearId,
            Title = request.Title.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = BudgetStatus.Draft
        };

        // Draft V1 — taslak versiyon; kalem versiyonları buna eklenir
        var draftVersion = new BudgetVersion
        {
            TenantId = request.TenantId,
            BudgetId = budget.Id,
            VersionNumber = 1,
            ValidFrom = null,
            ValidTo = null,
            PreviousVersionId = null,
            PublishedAt = null,
            PublishedBy = null,
            RevisionReason = null
        };
        budget.Versions.Add(draftVersion);

        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(budget.Id);
    }
}
