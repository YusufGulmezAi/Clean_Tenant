using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="ApproveJournalEntryCommand"/> handler.
/// <para>
/// Dual-control akışında onaylayan kullanıcı fişi oluşturandan farklı olmalıdır.
/// Onay fişi doğrudan Posted durumuna taşır.
/// </para>
/// </summary>
public sealed class ApproveJournalEntryCommandHandler
    : IRequestHandler<ApproveJournalEntryCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public ApproveJournalEntryCommandHandler(
        IMainDbContext db,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _sessionAccessor = sessionAccessor;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(
        ApproveJournalEntryCommand cmd,
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

        if (!settings.RequireApproval)
            return Result.Failure(
                Error.Failure("ACC-402", "Bu şirkette dual-control aktif değil."));

        // Fişi çek
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

        // Dual-control: fişi oluşturan onaylayamaz (CreatedBy interceptor tarafından set edilir)
        if (entry.CreatedBy == session.UserId)
            return Result.Failure(
                Error.Failure("ACC-403", "Dual control: fişi oluşturan kullanıcı onaylayamaz."));

        var now = DateTimeOffset.UtcNow;
        entry.Status = JournalEntryStatus.Posted;
        entry.PostedAt = now;
        entry.PostedBy = session.UserId;
        entry.ApprovedAt = now;
        entry.ApprovedBy = session.UserId;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
