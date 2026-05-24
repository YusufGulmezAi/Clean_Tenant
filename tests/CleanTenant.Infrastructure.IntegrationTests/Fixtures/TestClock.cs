using CleanTenant.SharedKernel.Time;

namespace CleanTenant.Infrastructure.IntegrationTests.Fixtures;

/// <summary>Testlerin zamanı kontrol edebildiği değiştirilebilir <see cref="IClock"/>.</summary>
public sealed class TestClock : IClock
{
    /// <summary>Geçerli sahte zaman; testler set eder.</summary>
    public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
