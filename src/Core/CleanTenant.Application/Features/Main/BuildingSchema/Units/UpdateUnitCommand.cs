using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.BuildingSchema;
using CleanTenant.SharedKernel.Common.Results;
using MediatR;

namespace CleanTenant.Application.Features.Main.BuildingSchema.Units;

/// <summary>
/// Mevcut bir Unit (Bağımsız Bölüm) bilgilerini günceller.
/// </summary>
/// <param name="UnitId">Güncellenecek BB.</param>
/// <param name="Number">BB numarası.</param>
/// <param name="NationalAddressCode">Ulusal adres numarataj kodu (opsiyonel).</param>
/// <param name="Type">BB tipi.</param>
/// <param name="SquareMeters">Brüt metrekare.</param>
/// <param name="LandShare">Arsa payı (pay; payda tüm BB'lerin toplamı).</param>
/// <param name="AllocatedArea">Tahsis alanı (opsiyonel).</param>
/// <param name="Floor">Bulunduğu kat.</param>
/// <param name="Orientation">Cephe yönü.</param>
/// <param name="Layout">Oda-salon düzeni.</param>
[RequirePermission("BuildingSchema.Manage")]
public sealed record UpdateUnitCommand(
    Guid UnitId,
    string Number,
    string? NationalAddressCode,
    UnitType Type,
    decimal SquareMeters,
    int LandShare,
    decimal? AllocatedArea,
    int Floor,
    Orientation Orientation,
    ApartmentLayout Layout) : IRequest<Result>;
