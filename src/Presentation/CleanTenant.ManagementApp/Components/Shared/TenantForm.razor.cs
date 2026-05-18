using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;
using Microsoft.AspNetCore.Components;
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

    /// <summary>Submit butonu yazısı.</summary>
    [Parameter] public string SubmitButtonText { get; set; } = "Kaydet";

    /// <summary>Submit sürerken UI'yı kilitleme bayrağı.</summary>
    [Parameter] public bool IsSubmitting { get; set; }

    /// <summary>İptal butonu için bağlantı (null → buton gizli).</summary>
    [Parameter] public string? CancelHref { get; set; }

    private MudForm _form = default!;
    private TenantFormValidator _validator = new(TenantFormMode.Create);

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

    private string IdentityNumberLabel => Model.LegalIdentityType switch
    {
        LegalIdentityType.Vkn => "VKN *",
        LegalIdentityType.Tckn => "TCKN *",
        LegalIdentityType.Ykn => "YKN *",
        _ => "Kimlik Numarası *",
    };

    private string IdentityNumberHelperText => Model.LegalIdentityType switch
    {
        LegalIdentityType.Vkn => "10 haneli, ilk hane 1-9 arasında.",
        LegalIdentityType.Tckn => "11 haneli, ilk hane 1-9 arasında.",
        LegalIdentityType.Ykn => "11 haneli, '99' ile başlamalı.",
        _ => string.Empty,
    };
}
