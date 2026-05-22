using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.BuildingSchema;

/// <summary>Yeni bağımsız bölüm oluşturulduğunda fırlatılır.</summary>
public sealed record UnitCreated(Guid UnitId, Guid BuildingId, Guid? BlockId) : IDomainEvent;
