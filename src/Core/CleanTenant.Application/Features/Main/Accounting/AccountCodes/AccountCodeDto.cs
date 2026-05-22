using CleanTenant.Domain.Tenant.Accounting.Enums;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>Hesap kodu liste elemanı — GetAccountCodesQuery dönüş tipi.</summary>
public record AccountCodeListItem(
    Guid Id,
    string Code,
    string? ParentCode,
    string Name,
    AccountLevel Level,
    AccountClass AccountClass,
    AccountType AccountType,
    bool IsActive,
    bool IsDetail,
    bool IsMonetary,
    bool IsRequired,
    AccountCodeSource Source);

/// <summary>Hesap kodu tam detay — GetAccountCodeDetailQuery dönüş tipi.</summary>
public record AccountCodeDetail(
    Guid Id,
    string Code,
    string? ParentCode,
    string Name,
    string? Description,
    AccountLevel Level,
    AccountClass AccountClass,
    AccountType AccountType,
    bool IsActive,
    bool IsDetail,
    bool IsMonetary,
    bool IsRequired,
    AccountCodeSource Source,
    string? TemplateCode,
    DateOnly? AcquisitionDate);
