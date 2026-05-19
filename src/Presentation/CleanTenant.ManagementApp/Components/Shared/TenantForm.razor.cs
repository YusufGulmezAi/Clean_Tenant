using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <para>
/// <see cref="TenantForm"/> — Yönetim oluşturma / düzenleme / kendi ayarları
/// için paylaşımlı form bileşeni. <see cref="TenantFormMode"/> parametresine
/// göre alanlar görünür / read-only / gizli render edilir.
/// </para>
/// <para>
/// <b>Validation:</b> <see cref="TenantFormValidator"/> mode-aware kuralları
/// FluentValidation üzerinden uygular; MudForm property-level error mesajını
/// <c>For</c> expression'ı sayesinde input altında gösterir.
/// </para>
/// <para>
/// <b>Submit:</b> <see cref="OnValidSubmit"/> yalnızca form valid ise tetiklenir.
/// Sayfa (Create/Edit/Settings) commande dönüştürüp Mediator'a gönderir.
/// </para>
/// </summary>
public sealed partial class TenantForm : ComponentBase
{
    /// <summary>Form'un binding hedefi.</summary>
    [Parameter, EditorRequired] public TenantFormModel Model { get; set; } = default!;

    /// <summary>Form modu — render ve validasyon kararlarını belirler.</summary>
    [Parameter, EditorRequired] public TenantFormMode Mode { get; set; }

    /// <summary>Form valid olduğunda tetiklenir.</summary>
    [Parameter] public EventCallback OnValidSubmit { get; set; }

    /// <summary>
    /// Submit butonu yazısı. Null/boş bırakılırsa <c>Common.Save</c> localizasyon
    /// anahtarına fallback olur.
    /// </summary>
    [Parameter] public string? SubmitButtonText { get; set; }

    /// <summary>Submit sürerken UI'yı kilitleme bayrağı.</summary>
    [Parameter] public bool IsSubmitting { get; set; }

    /// <summary>İptal butonu için bağlantı (null → buton gizli).</summary>
    [Parameter] public string? CancelHref { get; set; }

    [Inject] private IStringLocalizer Loc { get; set; } = default!;

    private MudForm _form = default!;
    private TenantFormValidator _validator = new(TenantFormMode.Create);

    /// <summary>SubmitButtonText parameter null/boş ise lokalize default'a düşer.</summary>
    private string ResolvedSubmitButtonText => string.IsNullOrWhiteSpace(SubmitButtonText)
        ? Loc["Common.Save"].Value
        : SubmitButtonText;

    /// <summary>MudForm.Validation parametresine bağlanan tipli delegate.</summary>
    public Func<object?, string, Task<IEnumerable<string>>> ValidateValue { get; }

    /// <summary>Ctor — delegate'i bir kez bağlar (her render'da yeniden ayırmaya gerek yok).</summary>
    public TenantForm()
    {
        ValidateValue = ValidateValueAsync;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _validator = new TenantFormValidator(Mode);
    }

    private async Task<IEnumerable<string>> ValidateValueAsync(object? value, string propertyName)
    {
        var context = ValidationContext<TenantFormModel>.CreateWithOptions(
            Model, x => x.IncludeProperties(propertyName));
        var result = await _validator.ValidateAsync(context);
        return result.Errors.Select(e => e.ErrorMessage);
    }

    /// <summary>Submit butonu tıklandığında çağrılır — önce validate, sonra callback.</summary>
    private async Task SubmitAsync()
    {
        await _form.Validate();
        if (_form.IsValid)
        {
            await OnValidSubmit.InvokeAsync();
        }
    }

    private bool IdentityReadOnly => Mode == TenantFormMode.Settings;
    private bool BillingReadOnly => Mode == TenantFormMode.Settings;
    private bool DedicatedDbReadOnly => Mode == TenantFormMode.Settings;
    private bool ShowAdminBlock => Mode == TenantFormMode.Create;
    private bool ShowSystemWriteAccess => Mode != TenantFormMode.Create;

    private string _lastFormattedPhone = string.Empty;

    private void FormatAdminPhone()
    {
        var raw = Model.AdminPhone ?? string.Empty;
        var digits = new string(raw.Where(char.IsDigit).ToArray());

        // Kullanıcı yapısal karakter (`)`, boşluk, `-`) sildi: raw kısaldı ama
        // rakam sayısı değişmedi → bir rakam daha sil ki geri gidebilelim.
        var prevDigits = new string(_lastFormattedPhone.Where(char.IsDigit).ToArray());
        if (raw.Length < _lastFormattedPhone.Length
            && digits.Length == prevDigits.Length
            && digits.Length > 0)
        {
            digits = digits[..^1];
        }

        _lastFormattedPhone = FormatPhone(digits);
        Model.AdminPhone = _lastFormattedPhone;
    }

    private static string FormatPhone(string digits)
    {
        if (digits.Length > 11) digits = digits[..11];
        int n = digits.Length;
        if (n == 0) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.Append(digits[0]);
        if (n == 1) return sb.ToString();
        sb.Append('(').Append(digits[1]);
        if (n == 2) return sb.ToString();
        sb.Append(digits[2]);
        if (n == 3) return sb.ToString();
        sb.Append(digits[3]).Append(')');
        if (n == 4) return sb.ToString();
        sb.Append(' ').Append(digits[4]);
        if (n == 5) return sb.ToString();
        sb.Append(digits[5]);
        if (n == 6) return sb.ToString();
        sb.Append(digits[6]);
        if (n == 7) return sb.ToString();
        sb.Append('-').Append(digits[7]);
        if (n == 8) return sb.ToString();
        sb.Append(digits[8]);
        if (n == 9) return sb.ToString();
        sb.Append('-').Append(digits[9]);
        if (n == 10) return sb.ToString();
        sb.Append(digits[10]);
        return sb.ToString();
    }

    private string IdentityNumberLabel => Model.LegalIdentityType switch
    {
        LegalIdentityType.Vkn => Loc["TenantForm.IdentityNumber.Vkn"].Value,
        LegalIdentityType.Tckn => Loc["TenantForm.IdentityNumber.Tckn"].Value,
        LegalIdentityType.Ykn => Loc["TenantForm.IdentityNumber.Ykn"].Value,
        _ => Loc["TenantForm.IdentityNumber.Fallback"].Value,
    };

    private string IdentityNumberHelperText => Model.LegalIdentityType switch
    {
        LegalIdentityType.Vkn => Loc["TenantForm.IdentityHelp.Vkn"].Value,
        LegalIdentityType.Tckn => Loc["TenantForm.IdentityHelp.Tckn"].Value,
        LegalIdentityType.Ykn => Loc["TenantForm.IdentityHelp.Ykn"].Value,
        _ => string.Empty,
    };
}
