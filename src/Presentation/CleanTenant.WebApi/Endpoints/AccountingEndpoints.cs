using CleanTenant.Application.Features.Main.Accounting.AccountCodes;
using CleanTenant.Application.Features.Main.Accounting.AccountingSettings;
using CleanTenant.Application.Features.Main.Accounting.FiscalYears;
using CleanTenant.Application.Features.Main.Accounting.JournalEntries;
using CleanTenant.Application.Features.Main.Accounting.Periods;
using CleanTenant.Application.Features.Main.Accounting.Reports;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using CleanTenant.SharedKernel.Common.Errors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanTenant.WebApi.Endpoints;

/// <summary>
/// Muhasebe modülü REST endpoint'leri — company-scoped.
/// Tüm path'ler /api/v1/companies/{companyId}/accounting altında toplanır.
/// </summary>
public static class AccountingEndpoints
{
    /// <summary>Muhasebe endpoint'lerini route'a bağlar.</summary>
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder routes)
    {
        var grp = routes
            .MapGroup("/api/v1/companies/{companyId:guid}/accounting")
            .RequireAuthorization();

        // ── Hesap Planı ───────────────────────────────────────────────────
        grp.MapGet("/chart-of-accounts", GetChartOfAccountsAsync)
           .WithName("GetChartOfAccounts");

        grp.MapPost("/chart-of-accounts", CreateAccountCodeAsync)
           .WithName("CreateAccountCode");

        grp.MapPut("/chart-of-accounts/{accountCodeId:guid}", UpdateAccountCodeAsync)
           .WithName("UpdateAccountCode");

        grp.MapPost("/chart-of-accounts/initialize", InitializeChartOfAccountsAsync)
           .WithName("InitializeChartOfAccounts");

        // ── Mali Yıllar ───────────────────────────────────────────────────
        grp.MapGet("/fiscal-years", GetFiscalYearsAsync)
           .WithName("GetFiscalYears");

        grp.MapPost("/fiscal-years", CreateFiscalYearAsync)
           .WithName("CreateFiscalYear");

        grp.MapPost("/fiscal-years/{fiscalYearId:guid}/close", CloseFiscalYearAsync)
           .WithName("CloseFiscalYear");

        // ── Dönemler ─────────────────────────────────────────────────────
        grp.MapGet("/periods", GetPeriodsAsync)
           .WithName("GetAccountingPeriods");

        grp.MapPost("/periods/{periodId:guid}/lock", LockPeriodAsync)
           .WithName("LockPeriod");

        grp.MapPost("/periods/{periodId:guid}/unlock", UnlockPeriodAsync)
           .WithName("UnlockPeriod");

        // ── Yevmiye Fişleri ───────────────────────────────────────────────
        grp.MapGet("/journal-entries", GetJournalEntriesAsync)
           .WithName("GetJournalEntries");

        grp.MapGet("/journal-entries/{entryId:guid}", GetJournalEntryDetailAsync)
           .WithName("GetJournalEntryDetail");

        grp.MapPost("/journal-entries", CreateJournalEntryAsync)
           .WithName("CreateJournalEntry");

        grp.MapPost("/journal-entries/{entryId:guid}/post", PostJournalEntryAsync)
           .WithName("PostJournalEntry");

        grp.MapPost("/journal-entries/{entryId:guid}/void", VoidJournalEntryAsync)
           .WithName("VoidJournalEntry");

        // ── Muhasebe Ayarları ─────────────────────────────────────────────
        grp.MapGet("/settings", GetAccountingSettingsAsync)
           .WithName("GetAccountingSettings");

        grp.MapPut("/settings", UpdateAccountingSettingsAsync)
           .WithName("UpdateAccountingSettings");

        grp.MapPost("/settings/activate", ActivateAccountingAsync)
           .WithName("ActivateAccounting");

        // ── Raporlar ─────────────────────────────────────────────────────
        grp.MapGet("/reports/trial-balance", GetTrialBalanceAsync)
           .WithName("GetTrialBalance");

        grp.MapGet("/reports/general-ledger", GetGeneralLedgerAsync)
           .WithName("GetGeneralLedger");

        grp.MapGet("/reports/balance-sheet", GetBalanceSheetAsync)
           .WithName("GetBalanceSheet");

        grp.MapGet("/reports/income-statement", GetIncomeStatementAsync)
           .WithName("GetIncomeStatement");

        grp.MapGet("/reports/account-statement", GetAccountStatementAsync)
           .WithName("GetAccountStatement");

        grp.MapGet("/reports/vat-summary", GetVatSummaryAsync)
           .WithName("GetVatSummary");

        grp.MapGet("/reports/budget-vs-actual", GetBudgetVsActualAsync)
           .WithName("GetBudgetVsActual");

        return routes;
    }

    // ── Hesap Planı Handler'ları ──────────────────────────────────────────────

    private static async Task<IResult> GetChartOfAccountsAsync(
        Guid companyId,
        [FromQuery] bool onlyActive,
        [FromQuery] bool onlyDetail,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAccountCodesQuery(companyId, onlyActive, onlyDetail), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateAccountCodeAsync(
        Guid companyId,
        [FromBody] CreateAccountCodeRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new CreateAccountCodeCommand(
            companyId, req.TenantId, req.Code, req.ParentCode, req.Name,
            req.Description, req.Level, req.AccountClass, req.AccountType,
            req.IsDetail, req.IsMonetary, req.AcquisitionDate);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetChartOfAccounts", new { companyId }, result.Value)
            : MapError(result.FirstError);
    }

    private static async Task<IResult> UpdateAccountCodeAsync(
        Guid companyId,
        Guid accountCodeId,
        [FromBody] UpdateAccountCodeRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new UpdateAccountCodeCommand(companyId, accountCodeId, req.Name, req.Description, req.IsMonetary, req.IsActive, req.IsDetail);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> InitializeChartOfAccountsAsync(
        Guid companyId,
        [FromBody] ActivateAccountingRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new InitializeChartOfAccountsCommand(companyId, req.TenantId), ct);
        return result.IsSuccess ? Results.Ok(new { imported = result.Value }) : MapError(result.FirstError);
    }

    // ── Mali Yıl Handler'ları ─────────────────────────────────────────────────

    private static async Task<IResult> GetFiscalYearsAsync(
        Guid companyId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetFiscalYearsQuery(companyId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateFiscalYearAsync(
        Guid companyId,
        [FromBody] CreateFiscalYearRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new CreateFiscalYearCommand(companyId, req.TenantId, req.Label, req.StartDate, req.EndDate, req.SetAsCurrent);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetFiscalYears", new { companyId }, result.Value)
            : MapError(result.FirstError);
    }

    private static async Task<IResult> CloseFiscalYearAsync(
        Guid companyId,
        Guid fiscalYearId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new CloseFiscalYearCommand(companyId, fiscalYearId), ct);
        return result.IsSuccess ? Results.Ok() : MapError(result.FirstError);
    }

    // ── Dönem Handler'ları ────────────────────────────────────────────────────

    private static async Task<IResult> GetPeriodsAsync(
        Guid companyId,
        [FromQuery] Guid fiscalYearId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPeriodsQuery(companyId, fiscalYearId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> LockPeriodAsync(
        Guid companyId,
        Guid periodId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new LockPeriodCommand(companyId, periodId), ct);
        return result.IsSuccess ? Results.Ok() : MapError(result.FirstError);
    }

    private static async Task<IResult> UnlockPeriodAsync(
        Guid companyId,
        Guid periodId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new UnlockPeriodCommand(companyId, periodId), ct);
        return result.IsSuccess ? Results.Ok() : MapError(result.FirstError);
    }

    // ── Yevmiye Handler'ları ──────────────────────────────────────────────────

    private static async Task<IResult> GetJournalEntriesAsync(
        Guid companyId,
        [FromQuery] Guid? periodId,
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] EntryType? entryType,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetJournalEntriesQuery(companyId, periodId, status, entryType, from, to,
                page < 1 ? 1 : page, pageSize < 1 ? 50 : pageSize), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetJournalEntryDetailAsync(
        Guid companyId,
        Guid entryId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetJournalEntryDetailQuery(companyId, entryId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> CreateJournalEntryAsync(
        Guid companyId,
        [FromBody] CreateJournalEntryRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new CreateJournalEntryCommand(
            companyId, req.TenantId, req.AccountingPeriodId, req.EntryType,
            req.EntryDate, req.Description, req.Reference, req.ReferenceId, req.Lines);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetJournalEntryDetail", new { companyId, entryId = result.Value!.Id }, result.Value)
            : MapError(result.FirstError);
    }

    private static async Task<IResult> PostJournalEntryAsync(
        Guid companyId,
        Guid entryId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new PostJournalEntryCommand(companyId, entryId), ct);
        return result.IsSuccess ? Results.Ok() : MapError(result.FirstError);
    }

    private static async Task<IResult> VoidJournalEntryAsync(
        Guid companyId,
        Guid entryId,
        [FromBody] VoidJournalEntryRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new VoidJournalEntryCommand(companyId, req.TenantId, entryId, req.VoidReason);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    // ── Ayar Handler'ları ─────────────────────────────────────────────────────

    private static async Task<IResult> GetAccountingSettingsAsync(
        Guid companyId,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAccountingSettingsQuery(companyId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> UpdateAccountingSettingsAsync(
        Guid companyId,
        [FromBody] UpdateAccountingSettingsRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var cmd = new UpdateAccountingSettingsCommand(companyId, req.RequireApproval, req.DefaultCurrency, req.VatPeriod, req.EDefterEnabled);
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> ActivateAccountingAsync(
        Guid companyId,
        [FromBody] ActivateAccountingRequest req,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ActivateAccountingCommand(companyId, req.TenantId), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    // ── Rapor Handler'ları ────────────────────────────────────────────────────

    private static async Task<IResult> GetTrialBalanceAsync(
        Guid companyId,
        [FromQuery] Guid fiscalYearId,
        [FromQuery] int? month,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetTrialBalanceQuery(companyId, fiscalYearId, month), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetGeneralLedgerAsync(
        Guid companyId,
        [FromQuery] string accountCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetGeneralLedgerQuery(companyId, accountCode, from, to), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetBalanceSheetAsync(
        Guid companyId,
        [FromQuery] DateOnly asOf,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetBalanceSheetQuery(companyId, asOf), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetIncomeStatementAsync(
        Guid companyId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetIncomeStatementQuery(companyId, from, to), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetAccountStatementAsync(
        Guid companyId,
        [FromQuery] string accountCode,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetAccountStatementQuery(companyId, accountCode, from, to), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetVatSummaryAsync(
        Guid companyId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetVatSummaryQuery(companyId, year, month), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    private static async Task<IResult> GetBudgetVsActualAsync(
        Guid companyId,
        [FromQuery] Guid fiscalYearId,
        [FromQuery] int? month,
        [FromServices] IMediator mediator,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetBudgetVsActualQuery(companyId, fiscalYearId, month), ct);
        return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.FirstError);
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    private static IResult MapError(Error error) =>
        Results.Json(new { error = error.Message, code = error.Code },
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Failure => StatusCodes.Status422UnprocessableEntity,
                ErrorType.Critical => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status400BadRequest,
            });
}

// ── Request DTO'ları ──────────────────────────────────────────────────────────

/// <summary>Hesap kodu oluşturma isteği.</summary>
public record CreateAccountCodeRequest(
    Guid TenantId,
    string Code,
    string? ParentCode,
    string Name,
    string? Description,
    CleanTenant.Domain.Tenant.Accounting.Enums.AccountLevel Level,
    CleanTenant.Domain.Tenant.Accounting.Enums.AccountClass AccountClass,
    CleanTenant.Domain.Tenant.Accounting.Enums.AccountType AccountType,
    bool IsDetail,
    bool IsMonetary,
    DateOnly? AcquisitionDate);

/// <summary>Hesap kodu güncelleme isteği.</summary>
public record UpdateAccountCodeRequest(
    string Name,
    string? Description,
    bool IsMonetary,
    bool IsActive,
    bool IsDetail);

/// <summary>Mali yıl oluşturma isteği.</summary>
public record CreateFiscalYearRequest(
    Guid TenantId,
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    bool SetAsCurrent = false);

/// <summary>Yevmiye fişi oluşturma isteği.</summary>
public record CreateJournalEntryRequest(
    Guid TenantId,
    Guid AccountingPeriodId,
    CleanTenant.Domain.Tenant.Accounting.Enums.EntryType EntryType,
    DateOnly EntryDate,
    string Description,
    string? Reference,
    Guid? ReferenceId,
    IReadOnlyList<CleanTenant.Application.Features.Main.Accounting.JournalEntries.JournalLineRequest> Lines);

/// <summary>Yevmiye fişi iptal isteği.</summary>
public record VoidJournalEntryRequest(
    Guid TenantId,
    string VoidReason);

/// <summary>Muhasebe ayarları güncelleme isteği.</summary>
public record UpdateAccountingSettingsRequest(
    bool RequireApproval,
    string DefaultCurrency,
    VatPeriod VatPeriod,
    bool EDefterEnabled);

/// <summary>Muhasebe aktivasyon isteği.</summary>
public record ActivateAccountingRequest(Guid TenantId);
