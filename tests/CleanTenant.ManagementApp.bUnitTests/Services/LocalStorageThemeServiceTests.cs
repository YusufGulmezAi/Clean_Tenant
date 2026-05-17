using Blazored.LocalStorage;
using CleanTenant.ManagementApp.Services;
using CleanTenant.ManagementApp.Themes;

namespace CleanTenant.ManagementApp.bUnitTests.Services;

/// <summary>
/// <see cref="LocalStorageThemeService"/> davranış testleri. Blazored.LocalStorage
/// NSubstitute ile mock'lanır; storage hit'leri ve event tetiklemeleri doğrulanır.
/// </summary>
public sealed class LocalStorageThemeServiceTests
{
    private const string PresetKey = "cleantenant.theme.preset";
    private const string DarkKey = "cleantenant.theme.dark";

    [Fact]
    public void Default_preset_KurumsalMavi_olmali()
    {
        var storage = Substitute.For<ILocalStorageService>();
        var service = new LocalStorageThemeService(storage);

        service.CurrentPreset.Should().Be(ThemePresetId.KurumsalMavi);
        service.IsDarkMode.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_storage_da_kayitli_preseti_yukler()
    {
        var storage = Substitute.For<ILocalStorageService>();
        storage.ContainKeyAsync(PresetKey).Returns(true);
        storage.GetItemAsync<int>(PresetKey).Returns((int)ThemePresetId.KoyuKurumsal);
        storage.ContainKeyAsync(DarkKey).Returns(true);
        storage.GetItemAsync<bool>(DarkKey).Returns(true);

        var service = new LocalStorageThemeService(storage);
        await service.InitializeAsync();

        service.CurrentPreset.Should().Be(ThemePresetId.KoyuKurumsal);
        service.IsDarkMode.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_storage_bos_ise_default_korunur()
    {
        var storage = Substitute.For<ILocalStorageService>();
        storage.ContainKeyAsync(Arg.Any<string>()).Returns(false);

        var service = new LocalStorageThemeService(storage);
        await service.InitializeAsync();

        service.CurrentPreset.Should().Be(ThemePresetId.KurumsalMavi);
        service.IsDarkMode.Should().BeFalse();
    }

    [Fact]
    public async Task SetPresetAsync_storage_a_yazar_ve_event_tetikler()
    {
        var storage = Substitute.For<ILocalStorageService>();
        var service = new LocalStorageThemeService(storage);
        var eventTriggered = false;
        service.ThemeChanged += () => eventTriggered = true;

        await service.SetPresetAsync(ThemePresetId.TeskilatsalYesil);

        service.CurrentPreset.Should().Be(ThemePresetId.TeskilatsalYesil);
        await storage.Received(1).SetItemAsync(PresetKey, (int)ThemePresetId.TeskilatsalYesil);
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleDarkModeAsync_state_degistirir_ve_storage_a_yazar()
    {
        var storage = Substitute.For<ILocalStorageService>();
        var service = new LocalStorageThemeService(storage);

        service.IsDarkMode.Should().BeFalse();
        await service.ToggleDarkModeAsync();
        service.IsDarkMode.Should().BeTrue();

        await storage.Received(1).SetItemAsync(DarkKey, true);
    }
}
