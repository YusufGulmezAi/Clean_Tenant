namespace CleanTenant.Domain.Tenant.Parties.Enums;

/// <summary>
/// İletişim kişisinin (UnitContact) BB ile ilişki rolü. Bu kişiler borçlu
/// olmaz ve tebligat almaz; yalnız acil durum / vekâlet iletişimi içindir.
/// </summary>
public enum ContactRole
{
    /// <summary>Mülk yöneticisi / vekil.</summary>
    PropertyManager = 0,

    /// <summary>Aile bireyi (eş, çocuk vb.).</summary>
    FamilyMember = 1,

    /// <summary>Avukat / hukuki temsilci.</summary>
    Lawyer = 2,

    /// <summary>Mirasçı.</summary>
    Heir = 3,

    /// <summary>Diğer.</summary>
    Other = 9
}
