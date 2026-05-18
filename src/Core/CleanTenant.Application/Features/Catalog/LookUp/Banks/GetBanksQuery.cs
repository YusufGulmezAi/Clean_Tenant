using CleanTenant.Application.Features.Catalog.Readers;
using MediatR;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

public sealed record GetBanksQuery : IRequest<Result<IReadOnlyList<BankListItem>>>;
