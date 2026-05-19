using MediatR;

namespace CleanTenant.Application.Features.System.Audit;

/// <summary>Audit girişlerini sayfalı döner.</summary>
public sealed record GetAuditEntriesQuery(AuditFilter Filter) : IRequest<AuditPageResult>;
