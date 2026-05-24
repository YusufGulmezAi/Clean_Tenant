using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Parties;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>BB'ye kiracı ekler. Aynı anda tek aktif kiracı varsayımı (TEN-003).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record AddUnitTenancyCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    Guid PartyId,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes = null) : IRequest<Result<Guid>>;

/// <summary><see cref="AddUnitTenancyCommand"/> kuralları.</summary>
public sealed class AddUnitTenancyCommandValidator : AbstractValidator<AddUnitTenancyCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitTenancyCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

/// <summary><see cref="AddUnitTenancyCommand"/> handler.</summary>
public sealed class AddUnitTenancyCommandHandler : IRequestHandler<AddUnitTenancyCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitTenancyCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddUnitTenancyCommand request, CancellationToken cancellationToken)
    {
        var check = await TenureGuards.ValidatePartyAndUnitAsync(_db, request.CompanyId, request.PartyId, request.UnitId, cancellationToken);
        if (check is { } error) return Result<Guid>.Failure(error);

        // Çakışan aktif kiracı (tarih aralığı kesişimi) — tek aktif kiracı kuralı
        var overlaps = await _db.UnitTenancies.AnyAsync(t =>
            t.UnitId == request.UnitId && !t.IsDeleted
            && t.StartDate <= (request.EndDate ?? DateOnly.MaxValue)
            && (t.EndDate ?? DateOnly.MaxValue) >= request.StartDate, cancellationToken);
        if (overlaps)
            return Result<Guid>.Failure(Error.Conflict("TEN-003", "Bu tarih aralığında zaten aktif bir kiracı var."));

        var entity = new UnitTenancy
        {
            TenantId = request.TenantId,
            UnitId = request.UnitId,
            PartyId = request.PartyId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };
        _db.UnitTenancies.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        // NOT: S3'te ReattributeAccrualResponsibilityCommand + GUARD tetiklenecek.
        return Result<Guid>.Success(entity.Id);
    }
}
