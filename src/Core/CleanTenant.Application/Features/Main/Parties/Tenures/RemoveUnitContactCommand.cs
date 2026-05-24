using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>İletişim kişisi kaydını siler (soft-delete).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record RemoveUnitContactCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid ContactId) : IRequest<Result>;

/// <summary><see cref="RemoveUnitContactCommand"/> kuralları.</summary>
public sealed class RemoveUnitContactCommandValidator : AbstractValidator<RemoveUnitContactCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitContactCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ContactId).NotEmpty();
    }
}

/// <summary><see cref="RemoveUnitContactCommand"/> handler.</summary>
public sealed class RemoveUnitContactCommandHandler : IRequestHandler<RemoveUnitContactCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitContactCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveUnitContactCommand request, CancellationToken cancellationToken)
    {
        var entity = await (
            from c in _db.UnitContacts
            join p in _db.Parties on c.PartyId equals p.Id
            where c.Id == request.ContactId && p.CompanyId == request.CompanyId && !c.IsDeleted
            select c).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("TEN-101", "İletişim kişisi kaydı bulunamadı."));

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
