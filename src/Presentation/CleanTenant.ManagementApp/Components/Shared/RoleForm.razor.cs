using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace CleanTenant.ManagementApp.Components.Shared;

public sealed partial class RoleForm
{
    private MudForm? _form;
    private readonly RoleFormValidator _validator = new();

    [Parameter]
    public RoleFormModel Model { get; set; } = new();

    /// <summary>
    /// Submit butonu yazısı. Null/boş bırakılırsa <c>Common.Save</c> localizasyon
    /// anahtarına fallback olur.
    /// </summary>
    [Parameter]
    public string? SubmitButtonText { get; set; }

    [Parameter]
    public string? CancelHref { get; set; }

    [Parameter]
    public bool IsSubmitting { get; set; }

    [Parameter]
    public bool ScopeReadOnly { get; set; }

    /// <summary>
    /// TenantArea sayfalarında Sistem scope seçeneğini dropdown'dan gizler.
    /// TenantAdmin sistem scope rol oluşturamaz (backend zaten reddeder, UI hint).
    /// </summary>
    [Parameter]
    public bool HideSystemScope { get; set; }

    [Parameter]
    public EventCallback<RoleFormModel> OnSubmit { get; set; }

    [Inject] private IStringLocalizer Loc { get; set; } = default!;

    /// <summary>SubmitButtonText parameter null/boş ise lokalize default'a düşer.</summary>
    private string ResolvedSubmitButtonText => string.IsNullOrWhiteSpace(SubmitButtonText)
        ? Loc["Common.Save"].Value
        : SubmitButtonText;

    public Func<object?, string, Task<IEnumerable<string>>> ValidateValue { get; }

    public RoleForm()
    {
        ValidateValue = ValidateValueAsync;
    }

    private async Task<IEnumerable<string>> ValidateValueAsync(object? value, string propertyName)
    {
        var context = ValidationContext<RoleFormModel>.CreateWithOptions(
            Model, x => x.IncludeProperties(propertyName));
        var result = await _validator.ValidateAsync(context);
        return result.Errors.Select(e => e.ErrorMessage);
    }

    private async Task SubmitAsync()
    {
        if (_form is null) return;

        await _form.Validate();

        if (!_form.IsValid) return;

        await OnSubmit.InvokeAsync(Model);
    }
}
