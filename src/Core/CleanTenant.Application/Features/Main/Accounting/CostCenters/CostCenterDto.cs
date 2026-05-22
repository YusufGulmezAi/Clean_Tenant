namespace CleanTenant.Application.Features.Main.Accounting.CostCenters;

/// <summary>Maliyet merkezi liste elemanı ve detay DTO'su.</summary>
public record CostCenterListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive);
