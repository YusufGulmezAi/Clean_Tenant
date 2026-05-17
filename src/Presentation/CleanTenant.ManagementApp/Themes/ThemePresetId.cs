namespace CleanTenant.ManagementApp.Themes;

/// <summary>
/// Kullanıcının seçebileceği 4 tema preset'i. v0.2.1'de runtime'da değiştirilir
/// ve <c>localStorage</c>'da kalıcılaştırılır.
/// </summary>
public enum ThemePresetId
{
    /// <summary>Kurumsal Mavi — primary #1976D2 + secondary #FF9800 (default).</summary>
    KurumsalMavi = 1,

    /// <summary>Teşkilatsal Yeşil — primary #2E7D32 + secondary #607D8B.</summary>
    TeskilatsalYesil = 2,

    /// <summary>MudBlazor Mor — primary #594AE2 + secondary #FF4081.</summary>
    MudBlazorMor = 3,

    /// <summary>Koyu Kurumsal — primary #263238 + secondary #00BCD4 (dark dostu).</summary>
    KoyuKurumsal = 4,
}
