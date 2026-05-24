namespace CleanTenant.Domain.Tenant.Parties.Enums;

/// <summary>Bir sorumluluk parçasının (AccrualResponsibilitySplit) taraf türü.</summary>
public enum ResponsibilityKind
{
    /// <summary>Malik (ev sahibi).</summary>
    Owner = 0,

    /// <summary>Kiracı.</summary>
    Tenant = 1
}
