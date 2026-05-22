using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Budgeting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ParticipationGroups;

/// <summary><see cref="CreateParticipationGroupCommand"/> handler.</summary>
public sealed class CreateParticipationGroupCommandHandler
    : IRequestHandler<CreateParticipationGroupCommand, Result<Guid>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateParticipationGroupCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateParticipationGroupCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        var duplicate = await _db.ParticipationGroups
            .AnyAsync(g => g.CompanyId == request.CompanyId
                        && g.Code == code
                        && !g.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<Guid>.Failure(
                Error.Conflict("BDG-501", $"'{code}' kodlu katılım grubu zaten mevcut."));

        var group = new ParticipationGroup
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true
        };

        _db.ParticipationGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(group.Id);
    }
}
