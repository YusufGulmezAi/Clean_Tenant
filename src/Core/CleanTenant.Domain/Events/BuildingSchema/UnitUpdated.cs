using CleanTenant.SharedKernel.Events;

namespace CleanTenant.Domain.Events.BuildingSchema;

/// <summary>Bağımsız bölüm güncellendiğinde fırlatılır.</summary>
public sealed record UnitUpdated(Guid UnitId) : IDomainEvent;
