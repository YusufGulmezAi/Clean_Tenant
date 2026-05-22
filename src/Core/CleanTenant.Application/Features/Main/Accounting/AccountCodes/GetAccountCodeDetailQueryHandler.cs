using CleanTenant.Application.Common.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="GetAccountCodeDetailQuery"/> handler.
/// </summary>
public sealed class GetAccountCodeDetailQueryHandler
    : IRequestHandler<GetAccountCodeDetailQuery, Result<AccountCodeDetail>>
{
    private readonly IMainDbContext _db;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public GetAccountCodeDetailQueryHandler(IMainDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<Result<AccountCodeDetail>> Handle(
        GetAccountCodeDetailQuery query,
        CancellationToken cancellationToken)
    {
        var ac = await _db.AccountCodes
            .Where(x => x.Id == query.AccountCodeId
                     && x.CompanyId == query.CompanyId
                     && !x.IsDeleted)
            .Select(x => new AccountCodeDetail(
                x.Id,
                x.Code,
                x.ParentCode,
                x.Name,
                x.Description,
                x.Level,
                x.AccountClass,
                x.AccountType,
                x.IsActive,
                x.IsDetail,
                x.IsMonetary,
                x.IsRequired,
                x.Source,
                x.TemplateCode,
                x.AcquisitionDate))
            .FirstOrDefaultAsync(cancellationToken);

        if (ac is null)
            return Result<AccountCodeDetail>.Failure(
                Error.NotFound("ACC-001", "Hesap kodu bulunamadı."));

        return Result<AccountCodeDetail>.Success(ac);
    }
}
