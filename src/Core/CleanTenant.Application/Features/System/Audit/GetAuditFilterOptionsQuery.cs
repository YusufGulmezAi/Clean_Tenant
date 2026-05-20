using MediatR;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>
/// Audit Explorer filtre panelinin autocomplete kaynaklarını (distinct
/// kullanıcı, entity tipi, Yönetim adı) DB'den döner. Tablonun mevcut
/// sayfasından değil tüm DB'den çekilir.
/// </summary>
public sealed record GetAuditFilterOptionsQuery() : IRequest<AuditFilterOptions>;
