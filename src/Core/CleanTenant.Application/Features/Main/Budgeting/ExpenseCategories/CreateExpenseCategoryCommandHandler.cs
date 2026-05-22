using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ExpenseCategories;

/// <summary>
/// <see cref="CreateExpenseCategoryCommand"/> handler. Kod benzersizliği +
/// parent doğrulama.
/// </summary>
public sealed class CreateExpenseCategoryCommandHandler
    : IRequestHandler<CreateExpenseCategoryCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateExpenseCategoryCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        // Kod benzersizliği (BDG-201)
        var duplicate = await _db.ExpenseCategories
            .AnyAsync(c => c.CompanyId == request.CompanyId
                        && c.Code == code
                        && !c.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-201", $"'{code}' kodlu gider kategorisi zaten mevcut."));

        // Parent kategori varsa: aynı şirkette mi?
        if (request.ParentCategoryId is { } parentId)
        {
            var parentOk = await _db.ExpenseCategories
                .AnyAsync(c => c.Id == parentId
                            && c.CompanyId == request.CompanyId
                            && !c.IsDeleted, cancellationToken);
            if (!parentOk)
                return Result<Guid>.Failure(
                    Error.NotFound("BDG-202", "Üst kategori bulunamadı."));
        }

        var category = new ExpenseCategory
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            ParentCategoryId = request.ParentCategoryId,
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            DisplayOrder = request.DisplayOrder
        };

        _db.ExpenseCategories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(category.Id);
    }
}
