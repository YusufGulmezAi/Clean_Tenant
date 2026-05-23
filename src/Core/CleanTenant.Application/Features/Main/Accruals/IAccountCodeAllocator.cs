using CleanTenant.Domain.Tenant.Budgeting.Enums;

namespace CleanTenant.Application.Features.Main.Accruals;

/// <summary>
/// <para>
/// Bütçe tahakkuğu için 120/600 alt hesap kodlarını üreten servis. İlk tahakkuk
/// anında çağrılır: <c>BudgetTypeMetadata</c> (Catalog) base kodlarını (örn. Aidat
/// → 120.01 / 600.01) okuyup şirkete özel bir sonraki alt hesabı oluşturur
/// (120.01.001, 120.01.002 …).
/// </para>
/// <para>
/// Üretilen hesaplar Main DB context'ine eklenir ama SaveChanges <b>çağırmaz</b>;
/// çağıran handler aynı transaction'da kaydeder. Parent zinciri (120, 120.01)
/// eksikse otomatik oluşturulur.
/// </para>
/// </summary>
public interface IAccountCodeAllocator
{
    /// <summary>
    /// Verilen bütçe tipi için yeni 120/600 alt hesap çiftini üretir ve context'e ekler.
    /// </summary>
    Task<AccountCodePair> AllocateBudgetAccountCodesAsync(
        Guid tenantId,
        Guid companyId,
        BudgetType type,
        string budgetTitle,
        CancellationToken cancellationToken);
}

/// <summary>Üretilen borç (120) ve gelir (600) hesap kodu id'leri.</summary>
public sealed record AccountCodePair(Guid ReceivableAccountCodeId, Guid IncomeAccountCodeId);
