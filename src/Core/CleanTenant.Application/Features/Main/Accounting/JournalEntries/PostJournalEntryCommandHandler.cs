using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="PostJournalEntryCommand"/> handler.
/// <para>
/// Tek imzalı muhasebeleştirme — dual-control kapalı şirketlerde kullanılır.
/// RequireApproval=true şirketlerde <c>ACC-404</c> hatası döner.
/// </para>
/// </summary>
public sealed class PostJournalEntryCommandHandler
    : IRequestHandler<PostJournalEntryCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public PostJournalEntryCommandHandler(
        IMainDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        PostJournalEntryCommand cmd,
        CancellationToken cancellationToken)
    {
        var session = _sessionAccessor.Current
            ?? throw new InvalidOperationException(
                "ICurrentSessionAccessor.Current null. Endpoint korumalı olmalı.");

        // Muhasebe ayarlarını kontrol et
        var settings = await _db.AccountingSettings
            .FirstOrDefaultAsync(s => s.CompanyId == cmd.CompanyId && !s.IsDeleted, cancellationToken);

        if (settings is null)
            return Result.Failure(
                Error.NotFound("ACC-005", "Muhasebe ayarları bulunamadı."));

        if (settings.RequireApproval)
            return Result.Failure(
                Error.Failure("ACC-404",
                    "Bu şirkette dual-control zorunlu. Fişi önce onaya gönderin."));

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
                Error.Failure("ACC-301", "Fiş zaten kesinleşmiş ya da uygunsuz durumda."));

        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAt = DateTimeOffset.UtcNow;
        entry.PostedBy = session.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
