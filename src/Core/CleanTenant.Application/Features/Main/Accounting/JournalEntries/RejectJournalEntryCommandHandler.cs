using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="RejectJournalEntryCommand"/> handler.
/// <para>
/// Onay bekleyen fişi taslak durumuna geri döndürür.
/// Böylece fiş düzenlenerek tekrar onaya gönderilebilir.
/// </para>
/// </summary>
public sealed class RejectJournalEntryCommandHandler
    : IRequestHandler<RejectJournalEntryCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RejectJournalEntryCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(
        RejectJournalEntryCommand cmd,
        CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(
                je => je.Id == cmd.EntryId
                   && je.CompanyId == cmd.CompanyId
                   && !je.IsDeleted, cancellationToken);

        if (entry is null)
            return Result.Failure(
                Error.NotFound("ACC-004", "Yevmiye fişi bulunamadı."));

        if (entry.Status != JournalEntryStatus.PendingApproval)
            return Result.Failure(
                Error.Failure("ACC-401", "Fiş onay bekliyor durumunda değil."));

        // Draft'a geri döndür — fiş yeniden düzenlenebilir
        entry.Status = JournalEntryStatus.Draft;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
