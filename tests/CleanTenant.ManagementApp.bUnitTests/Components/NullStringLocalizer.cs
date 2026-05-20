using Microsoft.Extensions.Localization;

namespace CleanTenant.ManagementApp.bUnitTests.Components;

/// <summary>
/// Test helper — <see cref="IStringLocalizer"/>'ı no-op olarak implement eder.
/// Verilen key'i raw string olarak döner (resourceNotFound=false). Validator
/// davranış testleri için kullanılır; mesaj içeriği değil, kural sonucu
/// (IsValid / PropertyName) doğrulanır.
/// </summary>
internal sealed class NullStringLocalizer : IStringLocalizer
{
    public static IStringLocalizer Instance { get; } = new NullStringLocalizer();

    public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(System.Globalization.CultureInfo.InvariantCulture, name, arguments), resourceNotFound: false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}
