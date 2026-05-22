using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>
/// Yeni mali yıl oluşturur ve otomatik olarak 12 aylık dönem oluşturur.
/// Çakışan dönem varsa ACC-205 hatası döner.
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record CreateFiscalYearCommand(
    Guid CompanyId,
    Guid TenantId,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    bool SetAsCurrent = false) : IRequest<Result<FiscalYearDetail>>;
