using CleanTenant.Application.Common.Auth;

namespace CleanTenant.Application.UnitTests.Auth;

/// <summary>
/// <see cref="LockoutDecision"/> testleri — etkin politika seçimi ve kilit kararı.
/// Tenant-başına hesap kilitleme (v0.2.14).
/// </summary>
public sealed class LockoutDecisionTests
{
    private static LockoutPolicy Policy(bool enabled = true, int max = 5, int minutes = 15)
        => new(enabled, max, TimeSpan.FromMinutes(minutes));

    // ── SelectEffective ──────────────────────────────────────────────────

    [Fact]
    public void Tek_tenant_politikasi_varsa_onu_secer()
    {
        var tenantPolicy = Policy(max: 3, minutes: 30);

        var result = LockoutDecision.SelectEffective([tenantPolicy]);

        result.Should().BeSameAs(tenantPolicy);
    }

    [Fact]
    public void Hic_tenant_yoksa_global_varsayilani_doner()
    {
        // System kullanıcı — tenant ataması yok.
        var result = LockoutDecision.SelectEffective([]);

        result.Should().Be(LockoutPolicy.Default);
    }

    [Fact]
    public void Birden_cok_tenant_varsa_global_varsayilani_doner()
    {
        // Çok-tenant'lı kullanıcı — hangi tenant'ın politikası belirsiz, default uygulanır.
        var result = LockoutDecision.SelectEffective([Policy(max: 3), Policy(max: 10)]);

        result.Should().Be(LockoutPolicy.Default);
    }

    // ── ShouldLock ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(4, false)] // eşiğin altında
    [InlineData(5, true)]  // eşikte
    [InlineData(6, true)]  // eşiğin üstünde
    public void Esige_ulasinca_kilitler(int failedCount, bool expected)
    {
        var policy = Policy(enabled: true, max: 5);

        LockoutDecision.ShouldLock(policy, failedCount).Should().Be(expected);
    }

    [Fact]
    public void Kilitleme_kapaliysa_esik_asilsa_bile_kilitlemez()
    {
        var policy = Policy(enabled: false, max: 5);

        LockoutDecision.ShouldLock(policy, failedCount: 99).Should().BeFalse();
    }

    [Fact]
    public void Tenant_ozel_dusuk_esik_uygulanir()
    {
        // Tenant 3 denemede kilitleme istiyorsa, global 5'ten önce kilitlenir.
        var policy = Policy(enabled: true, max: 3);

        LockoutDecision.ShouldLock(policy, failedCount: 3).Should().BeTrue();
        LockoutDecision.ShouldLock(policy, failedCount: 2).Should().BeFalse();
    }
}
