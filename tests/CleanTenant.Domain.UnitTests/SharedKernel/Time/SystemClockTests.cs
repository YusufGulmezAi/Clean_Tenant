using CleanTenant.SharedKernel.Time;

namespace CleanTenant.Domain.UnitTests.SharedKernel.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_gercek_UTC_zamani_doner()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow;

        var now = clock.UtcNow;

        var after = DateTimeOffset.UtcNow;
        now.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        now.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void IClock_NSubstitute_ile_mock_lanabiliyor()
    {
        var fixedDate = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(fixedDate);

        clock.UtcNow.Should().Be(fixedDate);
        clock.UtcNow.Should().Be(fixedDate);  // tekrar cagrida da sabit
    }
}
