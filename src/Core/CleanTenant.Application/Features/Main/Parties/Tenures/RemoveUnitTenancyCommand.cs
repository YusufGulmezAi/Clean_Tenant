using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>Kiracı tenure kaydını siler (soft-delete).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record RemoveUnitTenancyCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid TenancyId) : IRequest<Result>;

/// <summary><see cref="RemoveUnitTenancyCommand"/> kuralları.</summary>
public sealed class RemoveUnitTenancyCommandValidator : AbstractValidator<RemoveUnitTenancyCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitTenancyCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TenancyId).NotEmpty();
    }
}

/// <summary><see cref="RemoveUnitTenancyCommand"/> handler.</summary>
public sealed class RemoveUnitTenancyCommandHandler : IRequestHandler<RemoveUnitTenancyCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public RemoveUnitTenancyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(RemoveUnitTenancyCommand request, CancellationToken cancellationToken)
    {
        var entity = await (
            from t in _db.UnitTenancies
            join p in _db.Parties on t.PartyId equals p.Id
            where t.Id == request.TenancyId && p.CompanyId == request.CompanyId && !t.IsDeleted
            select t).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("TEN-101", "Kiracı kaydı bulunamadı."));

        entity.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
