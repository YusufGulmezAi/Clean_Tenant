using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="UpdateAccountCodeCommand"/> handler.
/// </summary>
public sealed class UpdateAccountCodeCommandHandler
    : IRequestHandler<UpdateAccountCodeCommand, Result<AccountCodeDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateAccountCodeCommandHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AccountCodeDetail>> Handle(
        UpdateAccountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var ac = await _db.AccountCodes
            .FirstOrDefaultAsync(x => x.Id == command.AccountCodeId
                                   && x.CompanyId == command.CompanyId
                                   && !x.IsDeleted, cancellationToken);

        if (ac is null)
            return Result<AccountCodeDetail>.Failure(
                Error.NotFound("ACC-001", "Hesap kodu bulunamadı."));

        ac.Name = command.Name;
        ac.Description = command.Description;
        ac.IsMonetary = command.IsMonetary;
        ac.IsActive = command.IsActive;
        ac.IsDetail = command.IsDetail;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<AccountCodeDetail>.Success(new AccountCodeDetail(
            ac.Id,
            ac.Code,
            ac.ParentCode,
            ac.Name,
            ac.Description,
            ac.Level,
            ac.AccountClass,
            ac.AccountType,
            ac.IsActive,
            ac.IsDetail,
            ac.IsMonetary,
            ac.IsRequired,
            ac.Source,
            ac.TemplateCode,
            ac.AcquisitionDate));
    }
}
