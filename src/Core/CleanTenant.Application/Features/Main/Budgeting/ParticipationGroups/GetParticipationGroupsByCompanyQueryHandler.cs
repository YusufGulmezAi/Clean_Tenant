using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Budgeting.ParticipationGroups;

/// <summary><see cref="GetParticipationGroupsByCompanyQuery"/> handler.</summary>
public sealed class GetParticipationGroupsByCompanyQueryHandler
    : IRequestHandler<GetParticipationGroupsByCompanyQuery, Result<IReadOnlyList<ParticipationGroupListItem>>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetParticipationGroupsByCompanyQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ParticipationGroupListItem>>> Handle(
        GetParticipationGroupsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        var q = _db.ParticipationGroups
            .Where(g => g.CompanyId == request.CompanyId && !g.IsDeleted);

        if (request.OnlyActive)
            q = q.Where(g => g.IsActive);

        var items = await q
            .OrderBy(g => g.Code)
            .Select(g => new ParticipationGroupListItem(
                g.Id,
                g.Code,
                g.Name,
                g.Description,
                g.IsActive,
                g.Memberships.Count(m => !m.IsDeleted)))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ParticipationGroupListItem>>.Success(items.AsReadOnly());
    }
}
