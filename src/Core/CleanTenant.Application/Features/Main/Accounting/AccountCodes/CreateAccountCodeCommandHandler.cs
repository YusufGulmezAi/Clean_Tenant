using CleanTenant.Application.Common.Persistence;
using CleanTenant.Domain.Tenant.Accounting;
using CleanTenant.Domain.Tenant.Accounting.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="CreateAccountCodeCommand"/> handler.
/// </summary>
public sealed class CreateAccountCodeCommandHandler
    : IRequestHandler<CreateAccountCodeCommand, Result<AccountCodeDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateAccountCodeCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AccountCodeDetail>> Handle(
        CreateAccountCodeCommand command,
        CancellationToken cancellationToken)
    {
        // Kod benzersizliği kontrolü
        var exists = await _db.AccountCodes
            .AnyAsync(ac => ac.CompanyId == command.CompanyId
                         && ac.Code == command.Code
                         && !ac.IsDeleted, cancellationToken);

        if (exists)
            return Result<AccountCodeDetail>.Failure(
                Error.Conflict("ACC-208", $"'{command.Code}' hesap kodu zaten mevcut."));

        var accountCode = new AccountCode
        {
            TenantId = command.TenantId,
            CompanyId = command.CompanyId,
            Code = command.Code,
            ParentCode = command.ParentCode,
            Name = command.Name,
            Description = command.Description,
            Level = command.Level,
            AccountClass = command.AccountClass,
            AccountType = command.AccountType,
            IsActive = true,
            IsDetail = command.IsDetail,
            IsMonetary = command.IsMonetary,
            IsRequired = false,
            Source = AccountCodeSource.CompanyDefined,
            AcquisitionDate = command.AcquisitionDate
        };

        _db.AccountCodes.Add(accountCode);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccountCodeDetail>.Success(new AccountCodeDetail(
            accountCode.Id,
            accountCode.Code,
            accountCode.ParentCode,
            accountCode.Name,
            accountCode.Description,
            accountCode.Level,
            accountCode.AccountClass,
            accountCode.AccountType,
            accountCode.IsActive,
            accountCode.IsDetail,
            accountCode.IsMonetary,
            accountCode.IsRequired,
            accountCode.Source,
            accountCode.TemplateCode,
            accountCode.AcquisitionDate));
    }
}
