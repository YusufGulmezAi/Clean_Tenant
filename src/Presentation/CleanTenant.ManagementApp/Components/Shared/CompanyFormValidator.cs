using FluentValidation;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// Site formu validasyon kuralları. Mode'a göre değişiklik gösterir
/// (Create'de tüm alanlar, Edit'te sadece düzenlenebilir alanlar).
/// </summary>
public sealed class CompanyFormValidator : AbstractValidator<CompanyFormModel>
{
    /// <summary>Validatör'ü moda göre başlatır.</summary>
    public CompanyFormValidator(CompanyFormMode mode)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Site adı zorunlu.")
            .MaximumLength(256).WithMessage("Site adı en fazla 256 karakter.");

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage("Yasal ad en fazla 512 karakter.");

        RuleFor(x => x.Vkn)
            .Matches(@"^[0-9]{10}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Vkn), ApplyConditionTo.CurrentValidator)
                .WithMessage("VKN 10 haneli rakamlardan oluşmalı.");

        RuleFor(x => x.Email)
            .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email), ApplyConditionTo.CurrentValidator)
                .WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter.");
    }
}
