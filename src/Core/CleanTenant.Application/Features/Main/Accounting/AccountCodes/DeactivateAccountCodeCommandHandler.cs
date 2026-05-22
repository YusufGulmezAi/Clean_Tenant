using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="DeactivateAccountCodeCommand"/> handler.
/// </summary>
public sealed class DeactivateAccountCodeCommandHandler
    : IRequestHandler<DeactivateAccountCodeCommand, Result<bool>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public DeactivateAccountCodeCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(
        DeactivateAccountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var ac = await _db.AccountCodes
            .FirstOrDefaultAsync(x => x.Id == command.AccountCodeId
                                   && x.CompanyId == command.CompanyId
                                   && !x.IsDeleted, cancellationToken);

        if (ac is null)
            return Result<bool>.Failure(
                Error.NotFound("ACC-001", "Hesap kodu bulunamadı."));

        if (ac.IsRequired)
            return Result<bool>.Failure(
                Error.Failure("ACC-209", "Zorunlu hesap kodu pasifleştirilemez."));

        // Aktif fiş satırı kontrolü — sadece Draft/PendingApproval durumundaki fişlerdeki satırlar engeller
        var hasActiveLines = await _db.JournalLines
            .AnyAsync(jl => jl.AccountCodeId == command.AccountCodeId
                         && jl.CompanyId == command.CompanyId
                         && !jl.IsDeleted
                         && jl.JournalEntry.Status != CleanTenant.Domain.Tenant.Accounting.Enums.JournalEntryStatus.Posted
                         && jl.JournalEntry.Status != CleanTenant.Domain.Tenant.Accounting.Enums.JournalEntryStatus.Voided,
                     cancellationToken);

        if (hasActiveLines)
            return Result<bool>.Failure(
                Error.Failure("ACC-210", "Hesap koduna bağlı aktif fiş satırı bulunuyor."));

        ac.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
