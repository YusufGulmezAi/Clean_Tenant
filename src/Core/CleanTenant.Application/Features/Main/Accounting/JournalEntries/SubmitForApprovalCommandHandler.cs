using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="SubmitForApprovalCommand"/> handler.
/// <para>
/// Yevmiye fişini Draft'tan PendingApproval durumuna taşır.
/// Şirkette RequireApproval aktif olmalıdır; aksi hâlde doğrudan
/// <see cref="PostJournalEntryCommand"/> kullanılmalıdır.
/// </para>
/// </summary>
public sealed class SubmitForApprovalCommandHandler
    : IRequestHandler<SubmitForApprovalCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public SubmitForApprovalCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(
        SubmitForApprovalCommand cmd,
        CancellationToken cancellationToken)
    {
        // Muhasebe ayarlarını kontrol et
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == cmd.CompanyId && !s.IsDeleted, cancellationToken);

        if (settings is null)
            return Result.Failure(
                Error.NotFound("ACC-005", "Muhasebe ayarları bulunamadı."));

        if (!settings.RequireApproval)
            return Result.Failure(
                Error.Failure("ACC-402", "Bu şirkette dual-control aktif değil. Fişi doğrudan muhasebeleştirin."));

        // Fişi çek
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(
                je => je.Id == cmd.EntryId
                   && je.CompanyId == cmd.CompanyId
                   && !je.IsDeleted, cancellationToken);

        if (entry is null)
            return Result.Failure(
                Error.NotFound("ACC-004", "Yevmiye fişi bulunamadı."));

        if (entry.Status != JournalEntryStatus.Draft)
            return Result.Failure(
                Error.Failure("ACC-301", "Yalnızca taslak (Draft) fişler onaya gönderilebilir."));

        entry.Status = JournalEntryStatus.PendingApproval;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
