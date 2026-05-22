using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.BuildingSchema;

/// <summary>Bağımsız bölüm silindiğinde fırlatılır.</summary>
public sealed record UnitDeleted(Guid UnitId, Guid BuildingId) : IDomainEvent;
