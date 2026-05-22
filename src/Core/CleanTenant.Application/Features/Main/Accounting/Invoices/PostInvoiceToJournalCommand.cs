using CleanTenant.Application.Common.Authorization;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.Invoices;

/// <summary>
/// Kayıtlı faturayı yevmiyeye aktarır — KDV satırları dahil otomatik fiş oluşturur.
/// <para>
/// <b>Gelen fatura (Incoming):</b>
/// Borç: gider hesabı (SubTotal) + İndirilecek KDV hesabı (VatAmount);
/// Alacak: 320.01.001 Satıcılar (TotalAmount).
/// </para>
/// <para>
/// <b>Giden fatura (Outgoing):</b>
/// Borç: 120.01.001 Alıcılar (TotalAmount);
/// Alacak: gelir hesabı (SubTotal) + Hesaplanan KDV hesabı (VatAmount).
/// </para>
/// </summary>
[RequirePermission("company.accounting.invoice.post")]
public sealed record PostInvoiceToJournalCommand(
    Guid InvoiceId,
    Guid CompanyId,
    Guid TenantId) : IRequest<Result<Guid>>;
