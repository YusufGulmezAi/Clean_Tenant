using CleanTenant.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Parties.Tenures;

/// <summary>Tenure komutları için ortak doğrulama yardımcıları.</summary>
internal static class TenureGuards
{
    /// <summary>
    /// Party şirkete ait + Unit mevcut (tenant-scoped) mu kontrol eder.
    /// Sorun varsa <see cref="Error"/>, yoksa null döner.
    /// </summary>
    public static async Task<Error?> ValidatePartyAndUnitAsync(
        IMainDbContext db, Guid companyId, Guid partyId, Guid unitId, CancellationToken ct)
    {
        var partyOk = await db.Parties.AnyAsync(p => p.Id == partyId && p.CompanyId == companyId && !p.IsDeleted, ct);
        if (!partyOk)
            return Error.NotFound("TEN-001", "Cari kişi bu sitede bulunamadı.");

        var unitOk = await db.Units.AnyAsync(u => u.Id == unitId && !u.IsDeleted, ct);
        if (!unitOk)
            return Error.NotFound("TEN-002", "Bağımsız bölüm bulunamadı.");

        return null;
    }
}
