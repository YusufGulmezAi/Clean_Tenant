using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.Permissions;

public sealed class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IAuthorizationCatalogReader _reader;

    public GetPermissionsQueryHandler(IAuthorizationCatalogReader reader)
    {
        _reader = reader;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _reader.GetAllPermissionsAsync(cancellationToken);
        return permissions
            .Select(p => new PermissionDto(p.Id, p.Code, p.Description, p.Module))
            .ToList()
            .AsReadOnly();
    }
}
