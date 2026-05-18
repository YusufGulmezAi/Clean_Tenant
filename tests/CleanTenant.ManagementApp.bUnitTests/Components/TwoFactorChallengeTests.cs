namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// <see cref="CleanTenant.ManagementApp.Components.Pages.TwoFactorChallenge"/> sayfa testleri.
/// v0.2.2.d sonrası PersistentComponentState dependency'si var; bUnit context'inde
/// internal ctor erişilemediği için bu testler InteractiveServer + cookie pattern'ı
/// için yeniden tasarlanacak. Şimdilik Skip — Faz 1.X UI test refactor'ünde
/// HttpContext mock'u + PersistentComponentState test harness'ı eklenecek.
/// </summary>
public sealed class TwoFactorChallengeTests
{
    [Fact(Skip = "v0.2.2.d: PersistentComponentState/HttpContext mock'u Faz 1.X'te eklenecek")]
    public void Token_yoksa_error_alert_render_edilmeli()
    {
        // No-op: bkz. sınıf XML doc.
    }
}
