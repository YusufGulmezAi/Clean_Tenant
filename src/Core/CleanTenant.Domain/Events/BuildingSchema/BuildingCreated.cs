using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.BuildingSchema;

/// <summary>Yeni yapı oluşturulduğunda fırlatılır.</summary>
public sealed record BuildingCreated(Guid BuildingId, Guid ParcelId) : IDomainEvent;
