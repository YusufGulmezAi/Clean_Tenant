using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.BudgetLines;

/// <summary>
/// <see cref="CreateBudgetLineCommand"/> handler. Kod benzersizliği + kategori +
/// (opsiyonel) TDHP hesap kodu doğrulama.
/// </summary>
public sealed class CreateBudgetLineCommandHandler
    : IRequestHandler<CreateBudgetLineCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateBudgetLineCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBudgetLineCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        // Kod benzersizliği (BDG-301)
        var duplicate = await _db.BudgetLines
            .AnyAsync(l => l.CompanyId == request.CompanyId
                        && l.Code == code
                        && !l.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-301", $"'{code}' kodlu bütçe kalemi zaten mevcut."));

        // Kategori doğrulama (BDG-302)
        var categoryOk = await _db.ExpenseCategories
            .AnyAsync(c => c.Id == request.ExpenseCategoryId
                        && c.CompanyId == request.CompanyId
                        && !c.IsDeleted, cancellationToken);
        if (!categoryOk)
            return Result<Guid>.Failure(
                Error.NotFound("BDG-302", "Gider kategorisi bulunamadı."));

        // Account code opsiyonel — verilirse yaprak hesap olmalı
        if (request.AccountCodeId is { } acId)
        {
            var ac = await _db.AccountCodes
                .FirstOrDefaultAsync(x => x.Id == acId
                                       && x.CompanyId == request.CompanyId
                                       && !x.IsDeleted, cancellationToken);
            if (ac is null)
                return Result<Guid>.Failure(
                    Error.NotFound("BDG-303", "Hesap kodu bulunamadı."));
            if (!ac.IsDetail)
                return Result<Guid>.Failure(
                    Error.Failure("BDG-304", "Yalnızca yaprak hesap kodu seçilebilir."));
        }

        var line = new BudgetLine
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            ExpenseCategoryId = request.ExpenseCategoryId,
            AccountCodeId = request.AccountCodeId,
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true,
            DisplayOrder = request.DisplayOrder
        };

        _db.BudgetLines.Add(line);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(line.Id);
    }
}
