using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// TDHP şablonunu kullanarak şirkete özgü hesap planını oluşturur.
/// Yalnızca bir kez çalışır; zaten aktifse hata döner.
/// </summary>
[RequirePermission("company.accounting.settings.manage")]
public sealed record InitializeChartOfAccountsCommand(
    Guid CompanyId,
    Guid TenantId) : IRequest<Result<int>>;
