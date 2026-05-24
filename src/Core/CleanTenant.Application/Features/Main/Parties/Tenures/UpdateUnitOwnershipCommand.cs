using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Parties.Responsibility;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>Malik tenure kaydını günceller (tarih, pay%, müteselsil).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record UpdateUnitOwnershipCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid OwnershipId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal SharePercent,
    bool IsJointAndSeveral,
    string? Notes = null) : IRequest<Result>;

/// <summary><see cref="UpdateUnitOwnershipCommand"/> kuralları.</summary>
public sealed class UpdateUnitOwnershipCommandValidator : AbstractValidator<UpdateUnitOwnershipCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUnitOwnershipCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.OwnershipId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.SharePercent).GreaterThan(0m).LessThanOrEqualTo(100m);
    }
}

/// <summary><see cref="UpdateUnitOwnershipCommand"/> handler.</summary>
public sealed class UpdateUnitOwnershipCommandHandler : IRequestHandler<UpdateUnitOwnershipCommand, Result>
{
    private readonly IMainDbContext _db;
    private readonly ISender _sender;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUnitOwnershipCommandHandler(IMainDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUnitOwnershipCommand request, CancellationToken cancellationToken)
    {
        var entity = await (
            from o in _db.UnitOwnerships
            join p in _db.Parties on o.PartyId equals p.Id
            where o.Id == request.OwnershipId && p.CompanyId == request.CompanyId && !o.IsDeleted
            select o).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("TEN-101", "Malik kaydı bulunamadı."));

        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.SharePercent = request.SharePercent;
        entity.IsJointAndSeveral = request.IsJointAndSeveral;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        await _sender.Send(new ReattributeAccrualResponsibilityCommand(
            request.TenantId, request.CompanyId, entity.UnitId), cancellationToken);

        return Result.Success();
    }
}
