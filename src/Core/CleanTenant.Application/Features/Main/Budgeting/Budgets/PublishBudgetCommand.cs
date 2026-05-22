using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Budgeting.Budgets;

/// <summary>
/// <para>
/// Taslak bütçenin Draft versiyonunu yayınlar (V1 olur). Bu komuttan sonra
/// versiyon immutable hâle gelir; tahakkuk üretimi (FAZ 6) mümkün olur.
/// </para>
/// <para>
/// Validations:
/// <list type="bullet">
///   <item>Bütçe Draft durumunda olmalı.</item>
///   <item>Bütçenin Draft versiyonunda en az bir <c>BudgetLineVersion</c> bulunmalı.</item>
///   <item><see cref="ValidFrom"/> mali yıl aralığı içinde olmalı.</item>
/// </list>
/// </para>
/// </summary>
[RequirePermission("tenant.budget.publish")]
public sealed record PublishBudgetCommand(
    Guid CompanyId,
    Guid BudgetId,
    DateOnly ValidFrom) : IRequest<Result<Guid>>;
