using CleanTenant.Application.Common.Auth;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// E2E testleri için değiştirilebilir <see cref="ICurrentSessionAccessor"/>.
/// Varsayılan <c>null</c> (sistem işlemi) — handler'lar GeneratedBy/CreatedBy'yi
/// null yazar.
/// </summary>
public sealed class TestSessionAccessor : ICurrentSessionAccessor
{
    /// <inheritdoc />
    public AuthSession? Current { get; set; }
}
