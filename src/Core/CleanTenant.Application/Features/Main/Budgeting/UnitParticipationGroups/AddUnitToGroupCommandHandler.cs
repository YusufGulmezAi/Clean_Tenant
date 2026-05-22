using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.UnitParticipationGroups;

/// <summary>
/// <see cref="AddUnitToGroupCommand"/> handler. Grup + BB doğrulama,
/// aktif duplicate kontrolü.
/// </summary>
public sealed class AddUnitToGroupCommandHandler
    : IRequestHandler<AddUnitToGroupCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitToGroupCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddUnitToGroupCommand request, CancellationToken cancellationToken)
    {
        // Grup var mı + aktif + bu şirkete mi ait
        var groupOk = await _db.ParticipationGroups
            .AnyAsync(g => g.Id == request.ParticipationGroupId
                        && g.CompanyId == request.CompanyId
                        && g.IsActive
                        && !g.IsDeleted, cancellationToken);
        if (!groupOk)
            return Result<Guid>.Failure(Error.NotFound("BDG-600", "Aktif katılım grubu bulunamadı."));

        // BB var mı + bu şirkete mi ait (Unit → Building → Company zinciri kontrol edilebilir,
        // şimdilik Unit'in TenantId filtresi + var olma yeterli)
        var unitOk = await _db.Units
            .AnyAsync(u => u.Id == request.UnitId && !u.IsDeleted, cancellationToken);
        if (!unitOk)
            return Result<Guid>.Failure(Error.NotFound("BDG-602", "Bağımsız Bölüm bulunamadı."));

        // Aktif duplicate kontrolü (BDG-601)
        var duplicate = await _db.UnitParticipationGroups
            .AnyAsync(m => m.ParticipationGroupId == request.ParticipationGroupId
                        && m.UnitId == request.UnitId
                        && !m.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-601", "BB bu grupta zaten kayıtlı."));

        // Sanity: ValidFrom <= ValidTo
        if (request.ValidTo is { } to && to < request.ValidFrom)
            return Result<Guid>.Failure(
                Error.Failure("BDG-603", "Bitiş tarihi başlangıçtan önce olamaz."));

        var membership = new UnitParticipationGroup
        {
            TenantId = request.TenantId,
            ParticipationGroupId = request.ParticipationGroupId,
            UnitId = request.UnitId,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        _db.UnitParticipationGroups.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(membership.Id);
    }
}
