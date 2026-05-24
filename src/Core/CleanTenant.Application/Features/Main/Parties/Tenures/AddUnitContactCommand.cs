using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Parties;
using CleanTenant.Domain.Tenant.Parties.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>BB'ye iletişim kişisi ekler (borçlu olmaz).</summary>
[RequirePermission("tenant.tenure.manage")]
public sealed record AddUnitContactCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    Guid PartyId,
    ContactRole ContactRole,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes = null) : IRequest<Result<Guid>>;

/// <summary><see cref="AddUnitContactCommand"/> kuralları.</summary>
public sealed class AddUnitContactCommandValidator : AbstractValidator<AddUnitContactCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitContactCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly));
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
    }
}

/// <summary><see cref="AddUnitContactCommand"/> handler.</summary>
public sealed class AddUnitContactCommandHandler : IRequestHandler<AddUnitContactCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitContactCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddUnitContactCommand request, CancellationToken cancellationToken)
    {
        var check = await TenureGuards.ValidatePartyAndUnitAsync(_db, request.CompanyId, request.PartyId, request.UnitId, cancellationToken);
        if (check is { } error) return Result<Guid>.Failure(error);

        var entity = new UnitContact
        {
            TenantId = request.TenantId,
            UnitId = request.UnitId,
            PartyId = request.PartyId,
            ContactRole = request.ContactRole,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };
        _db.UnitContacts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}
