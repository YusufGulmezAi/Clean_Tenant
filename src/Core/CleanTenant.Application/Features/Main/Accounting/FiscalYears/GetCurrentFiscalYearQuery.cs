using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.FiscalYears;

/// <summary>Şirketin cari mali yılını döner.</summary>
[RequirePermission("company.accounting.account-plan.read")]
public sealed record GetCurrentFiscalYearQuery(
    Guid CompanyId) : IRequest<Result<FiscalYearDetail>>;
