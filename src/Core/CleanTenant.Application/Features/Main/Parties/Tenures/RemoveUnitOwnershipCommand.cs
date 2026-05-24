using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>Malik tenure kaydını siler (soft-delete).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record RemoveUnitOwnershipCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid OwnershipId) : IRequest<Result>;

/// <summary><see cref="RemoveUnitOwnershipCommand"/> kuralları.</summary>
public sealed class RemoveUnitOwnershipCommandValidator : AbstractValidator<RemoveUnitOwnershipCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitOwnershipCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.OwnershipId).NotEmpty();
    }
}

/// <summary><see cref="RemoveUnitOwnershipCommand"/> handler.</summary>
public sealed class RemoveUnitOwnershipCommandHandler : IRequestHandler<RemoveUnitOwnershipCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitOwnershipCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveUnitOwnershipCommand request, CancellationToken cancellationToken)
    {
        var entity = await (
            from o in _db.UnitOwnerships
            join p in _db.Parties on o.PartyId equals p.Id
            where o.Id == request.OwnershipId && p.CompanyId == request.CompanyId && !o.IsDeleted
            select o).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("TEN-101", "Malik kaydı bulunamadı."));

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
