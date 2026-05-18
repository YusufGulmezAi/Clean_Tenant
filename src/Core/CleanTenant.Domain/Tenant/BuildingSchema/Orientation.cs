namespace CleanTenant.Domain.Tenant.BuildingSchema;

/// <summary>
/// Bağımsız bölümün ana cephesinin baktığı yön.
/// </summary>
public enum Orientation
{
    /// <summary>Yön belirsiz veya tanımsız.</summary>
    Unknown = 0,

    /// <summary>Kuzey (N).</summary>
    North = 1,

    /// <summary>Güney (S).</summary>
    South = 2,

    /// <summary>Doğu (E).</summary>
    East = 3,

    /// <summary>Batı (W).</summary>
    West = 4,

    /// <summary>Kuzeydoğu (NE).</summary>
    NorthEast = 5,

    /// <summary>Kuzeybatı (NW).</summary>
    NorthWest = 6,

    /// <summary>Güneydoğu (SE).</summary>
    SouthEast = 7,

    /// <summary>Güneybatı (SW).</summary>
    SouthWest = 8,
}
