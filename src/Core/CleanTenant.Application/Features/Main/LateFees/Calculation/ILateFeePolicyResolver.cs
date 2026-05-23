using CleanTenant.Domain.Tenant.LateFees;

namespace CleanTenant.Application.Features.Main.LateFees.Calculation;

/// <summary>
/// Bir borç için etkin gecikme faizi politikasını çözer. Hiyerarşi: önce bütçe
/// override (<c>BudgetId</c> eşleşen), yoksa şirket-geneli varsayılan
/// (<c>BudgetId = null</c>). Hiçbiri yoksa <c>null</c>.
/// </summary>
public interface ILateFeePolicyResolver
{
    /// <summary>
    /// <paramref name="companyPolicies"/> şirketin tüm (tercihen aktif) politikalarıdır.
    /// </summary>
    LateFeePolicy? Resolve(IReadOnlyCollection<LateFeePolicy> companyPolicies, Guid? budgetId);
}
