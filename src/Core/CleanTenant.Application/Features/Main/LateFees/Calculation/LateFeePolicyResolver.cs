using CleanTenant.Domain.Tenant.LateFees;

namespace CleanTenant.Application.Features.Main.LateFees.Calculation;

/// <summary><see cref="ILateFeePolicyResolver"/> implementasyonu (saf, in-memory).</summary>
public sealed class LateFeePolicyResolver : ILateFeePolicyResolver
{
    /// <inheritdoc />
    public LateFeePolicy? Resolve(IReadOnlyCollection<LateFeePolicy> companyPolicies, Guid? budgetId)
    {
        // Önce bütçe override
        if (budgetId is { } bid)
        {
            var overridePolicy = companyPolicies.FirstOrDefault(p => p.IsActive && p.BudgetId == bid);
            if (overridePolicy is not null)
                return overridePolicy;
        }

        // Sonra şirket-geneli varsayılan
        return companyPolicies.FirstOrDefault(p => p.IsActive && p.BudgetId is null);
    }
}
