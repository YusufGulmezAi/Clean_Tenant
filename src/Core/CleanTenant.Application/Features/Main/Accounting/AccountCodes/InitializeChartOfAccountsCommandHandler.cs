using CleanTenant.Application.Features.Main.Accounting.Provisioning;
using MediatR;

namespace CleanTenant.Application.Features.Main.Accounting.AccountCodes;

/// <summary>
/// <see cref="InitializeChartOfAccountsCommand"/> handler.
/// <para>
/// TDHP hesap planını şirkete idempotent olarak ekler ve muhasebe ayarlarını
/// aktifleştirir. Asıl iş <see cref="IChartOfAccountsProvisioner"/>'da (tek kaynak);
/// aynı mantık şirketin ilk Mali Dönemi açılırken de kullanılır.
/// </para>
/// </summary>
public sealed class InitializeChartOfAccountsCommandHandler
    : IRequestHandler<InitializeChartOfAccountsCommand, Result<int>>
{
    private readonly IChartOfAccountsProvisioner _provisioner;

    /// <summary>DI bağımlılıklarını alır.</summary>
    public InitializeChartOfAccountsCommandHandler(IChartOfAccountsProvisioner provisioner)
    {
        _provisioner = provisioner;
    }

    /// <inheritdoc />
    public async Task<Result<int>> Handle(
        InitializeChartOfAccountsCommand command,
        CancellationToken cancellationToken)
    {
        var count = await _provisioner.EnsureStandardChartAsync(
            command.CompanyId, command.TenantId, cancellationToken);
        return Result<int>.Success(count);
    }
}
