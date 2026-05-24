using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <para>
/// Bütçe yenileme — mevcut bir bütçeyi yeni mali yıl/döneme <b>kopyalar</b> ve
/// yeni bir <c>Draft</c> bütçe (V1) üretir. Aynı şirket içinde olduğundan kategori,
/// kalem ve katılım grupları ID ile yeniden kullanılır (yeni tanım üretilmez).
/// </para>
/// <para>
/// Kopyalanan tasarım kaynağı: bütçe yayınlıysa aktif versiyon (CurrentVersion),
/// değilse tek taslak versiyon. Kalem versiyonları + tutarları + taksit planı
/// kopyalanır; taksit ayları yeni döneme ötelenir. Versiyon zinciri V1'e sıfırlanır;
/// hesap kodları boş kalır (ilk tahakkukta otomatik açılır).
/// </para>
/// </summary>
[RequirePermission("tenant.budget.edit")]
public sealed record CloneBudgetCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid SourceBudgetId,
    Guid NewFiscalYearId,
    string NewTitle,
    int? PeriodStartYear = null,
    int? PeriodStartMonth = null,
    int? PeriodEndYear = null,
    int? PeriodEndMonth = null) : IRequest<Result<Guid>>;
