using CleanTenant.Application.Common.Auth;
using CleanTenant.Application.Common.Authorization;
using CleanTenant.Application.Common.Caching;
using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Catalog.Roles;

/// <summary>
/// Rol metadata güncelleme handler'ı. v0.2.8.c'den itibaren built-in koruma
/// ve sahiplik (TenantId/CompanyId) kontrolleri uygulanır.
/// </summary>
public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly ICatalogDbContext _db;
    private readonly ICacheInvalidator _cacheInvalidator;
    private readonly ICurrentSessionAccessor _sessionAccessor;

    public UpdateRoleCommandHandler(
        ICatalogDbContext db,
        ICacheInvalidator cacheInvalidator,
        ICurrentSessionAccessor sessionAccessor)
    {
        _db = db;
        _cacheInvalidator = cacheInvalidator;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<Unit> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (role is null)
            throw new InvalidOperationException($"Rol bulunamadı: {request.Id}");

        RoleAccessGuard.EnsureCanManageRole(_sessionAccessor.Current, role);

        // Built-in rollerin adı/açıklaması değiştirilemez (yalnız izinleri düzenlenir).
        // ÖNEMLİ: Yalnız gerçek bir DEĞİŞİKLİK varsa reddet. İzin düzenleme akışında
        // UI bu komutu (ad/açıklama değişmese de) gönderebiliyor; aynı değerlerle gelen
        // çağrıyı no-op kabul et ki "sadece izin" kaydetme bloklanmasın.
        if (role.IsBuiltIn)
        {
            var nameChanged = !string.Equals(role.Name, request.Name, StringComparison.Ordinal);
            var descChanged = !string.Equals(
                role.Description ?? string.Empty, request.Description ?? string.Empty, StringComparison.Ordinal);

            if (nameChanged || descChanged)
                throw new InvalidOperationException("Built-in rollerin adı/açıklaması değiştirilemez (sadece izinleri).");

            return Unit.Value; // değişiklik yok → metadata güncellemesi yapılmaz
        }

        role.Name = request.Name;
        role.Description = request.Description;

        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidator.InvalidateRoleAsync(request.Id, cancellationToken);

        return Unit.Value;
    }
}
