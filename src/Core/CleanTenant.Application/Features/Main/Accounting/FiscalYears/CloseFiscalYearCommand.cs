using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// Mali yılı kalıcı olarak kapatır (ClosedPermanent).
/// Tüm dönemler Locked durumunda olmalı; aksi hâlde ACC-204 hatası döner.
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record CloseFiscalYearCommand(
    Guid CompanyId,
    Guid FiscalYearId) : IRequest<Result<bool>>;
