using CleanTenant.Application.Common.Authorization;
using CleanTenant.Domain.Tenant.Collections.Enums;
using MediatR;

namespace CleanTenant.Application.Features.Main.Collections.RefundAdvance;

/// <summary>
/// Bir BB'nin avans (fazla ödeme) bakiyesinden sakine nakit iade yapar.
/// <para>
/// Yevmiye: Borç 120 (avansın durduğu alacak hesabı) / Alacak 100-102 (kasa-banka).
/// Kaynak tahsilatların <c>UnallocatedAmount</c>'u (eski→yeni) düşürülür.
/// <c>AccountingSettings.RequireApproval</c> true ise fiş <c>PendingApproval</c> açılır.
/// </para>
/// </summary>
[RequirePermission("tenant.advance.refund")]
public sealed record RefundAdvanceCommand(
    Guid TenantId,
    Guid CompanyId,
    Guid UnitId,
    decimal Amount,
    DateOnly RefundDate,
    Guid CashAccountCodeId,
    PaymentMethod Method,
    string? Reference = null,
    string? Description = null) : IRequest<Result<RefundResult>>;

/// <summary>Avans iade sonucu.</summary>
public sealed record RefundResult(
    Guid RefundId,
    decimal RefundedAmount,
    decimal RemainingAdvance,
    bool PendingApproval);
