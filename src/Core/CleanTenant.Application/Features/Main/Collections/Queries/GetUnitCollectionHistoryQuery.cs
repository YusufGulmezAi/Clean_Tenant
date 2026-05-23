using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Collections.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.Queries;

/// <summary>Bir Bağımsız Bölümün tahsilat geçmişini (opsiyonel tarih aralığı) listeler.</summary>
[RequirePermission("tenant.collection.view")]
public sealed record GetUnitCollectionHistoryQuery(
    Guid CompanyId,
    Guid UnitId,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<Result<IReadOnlyList<CollectionListItem>>>;

/// <summary>Tahsilat başlığı liste öğesi (makbuz özeti).</summary>
public sealed record CollectionListItem(
    Guid Id,
    string UrlCode,
    Guid UnitId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    decimal UnallocatedAmount,
    int AllocationCount,
    Guid? JournalEntryId,
    DateTimeOffset RecordedAt);
