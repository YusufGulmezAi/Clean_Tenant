using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Application.Features.Main.Parties.Responsibility;
using CleanTenant.Domain.Tenant.Parties;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>BB'ye malik (pay% + müteselsil) ekler.</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record AddUnitOwnershipCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    Guid PartyId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal SharePercent,
    bool IsJointAndSeveral,
    string? Notes = null) : IRequest<Result<Guid>>;

/// <summary><see cref="AddUnitOwnershipCommand"/> kuralları.</summary>
public sealed class AddUnitOwnershipCommandValidator : AbstractValidator<AddUnitOwnershipCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitOwnershipCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.SharePercent).GreaterThan(0m).LessThanOrEqualTo(100m);
    }
}

/// <summary><see cref="AddUnitOwnershipCommand"/> handler.</summary>
public sealed class AddUnitOwnershipCommandHandler : IRequestHandler<AddUnitOwnershipCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;
    private readonly ISender _sender;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitOwnershipCommandHandler(IMainDbContext db, ISender sender)
    {
        _db = db;
        _sender = sender;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddUnitOwnershipCommand request, CancellationToken cancellationToken)
    {
        var check = await TenureGuards.ValidatePartyAndUnitAsync(_db, request.CompanyId, request.PartyId, request.UnitId, cancellationToken);
        if (check is { } error) return Result<Guid>.Failure(error);

        var entity = new UnitOwnership
        {
            TenantId = request.TenantId,
            UnitId = request.UnitId,
            PartyId = request.PartyId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SharePercent = request.SharePercent,
            IsJointAndSeveral = request.IsJointAndSeveral,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };
        _db.UnitOwnerships.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // Tenure değişti → etkilenen (temiz) tahakkukların sorumluluğunu yeniden hesapla.
        await _sender.Send(new ReattributeAccrualResponsibilityCommand(
            request.TenantId, request.CompanyId, request.UnitId), cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
