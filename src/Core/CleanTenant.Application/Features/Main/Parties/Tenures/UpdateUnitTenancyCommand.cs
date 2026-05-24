using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>Kiracı tenure kaydını günceller (giriş/çıkış tarihi).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record UpdateUnitTenancyCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid TenancyId,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes = null) : IRequest<Result>;

/// <summary><see cref="UpdateUnitTenancyCommand"/> kuralları.</summary>
public sealed class UpdateUnitTenancyCommandValidator : AbstractValidator<UpdateUnitTenancyCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUnitTenancyCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.TenancyId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

/// <summary><see cref="UpdateUnitTenancyCommand"/> handler.</summary>
public sealed class UpdateUnitTenancyCommandHandler : IRequestHandler<UpdateUnitTenancyCommand, Result>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateUnitTenancyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateUnitTenancyCommand request, CancellationToken cancellationToken)
    {
        var entity = await (
            from t in _db.UnitTenancies
            join p in _db.Parties on t.PartyId equals p.Id
            where t.Id == request.TenancyId && p.CompanyId == request.CompanyId && !t.IsDeleted
            select t).FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
            return Result.Failure(Error.NotFound("TEN-101", "Kiracı kaydı bulunamadı."));

        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        // NOT: S3'te ReattributeAccrualResponsibilityCommand + GUARD tetiklenecek (çıkış tarihi değişimi).
        return Result.Success();
    }
}
