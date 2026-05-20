using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// Site oluşturma / düzenleme paylaşımlı form bileşeni.
/// <see cref="CompanyFormMode"/> parametresine göre alanlar render edilir.
/// </summary>
public sealed partial class CompanyForm : ComponentBase
{
    /// <summary>Form'un binding hedefi.</summary>
    [Parameter, EditorRequired] public CompanyFormModel Model { get; set; } = default!;

    /// <summary>Form modu — render ve validasyon kararlarını belirler.</summary>
    [Parameter, EditorRequired] public CompanyFormMode Mode { get; set; }

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

    /// <summary>
    /// İçerideki Kaydet/İptal aksiyon barını gizler. Tab'lı edit sayfası gibi
    /// dış konteynerlerin kendi butonlarını yerleştirmesi için kullanılır;
    /// dışarıdan <see cref="SubmitAsync"/> çağrısı ile gönderim tetiklenir.
    /// </summary>
    [Parameter] public bool HideActions { get; set; }

    [Inject] private IStringLocalizer Loc { get; set; } = default!;

    private MudForm _form = default!;
    private CompanyFormValidator? _validator;

    /// <summary>SubmitButtonText parameter null/boş ise lokalize default'a düşer.</summary>
    private string ResolvedSubmitButtonText => string.IsNullOrWhiteSpace(SubmitButtonText)
        ? Loc["Common.Save"].Value
        : SubmitButtonText;

    /// <summary>MudForm.Validation parametresine bağlanan tipli delegate.</summary>
    public Func<object?, string, Task<IEnumerable<string>>> ValidateValue { get; }

    /// <summary>Ctor — delegate'i bir kez bağlar.</summary>
    public CompanyForm()
    {
        ValidateValue = ValidateValueAsync;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _validator = new CompanyFormValidator(Mode, Loc);
    }

    private async Task<IEnumerable<string>> ValidateValueAsync(object? value, string propertyName)
    {
        if (_validator is null) return Array.Empty<string>();
        var context = ValidationContext<CompanyFormModel>.CreateWithOptions(
            Model, x => x.IncludeProperties(propertyName));
        var result = await _validator.ValidateAsync(context);
        return result.Errors.Select(e => e.ErrorMessage);
    }

    /// <summary>
    /// Form'u doğrular ve geçerliyse <see cref="OnValidSubmit"/> callback'ini tetikler.
    /// Public — <see cref="HideActions"/> kullanan dış konteynerler (örn. tab'lı detay
    /// sayfası) bu metodu @ref üzerinden çağırır.
    /// </summary>
    public async Task SubmitAsync()
    {
        await _form.Validate();
        if (_form.IsValid)
        {
            await OnValidSubmit.InvokeAsync();
        }
    }
}
