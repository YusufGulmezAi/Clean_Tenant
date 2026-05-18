using FluentValidation;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <see cref="UpdateCompanyCommand"/> validation kuralları.
/// </summary>
public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Site kimliği zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Site adı zorunlu.")
            .MaximumLength(256).WithMessage("Site adı en fazla 256 karakter.");

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage("Yasal ad en fazla 512 karakter.");

        RuleFor(x => x.Vkn)
            .Matches(@"^[1-9][0-9]{9}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Vkn), ApplyConditionTo.CurrentValidator)
                .WithMessage("VKN 10 haneli, ilk hane 1-9 arasında olmalı.");

        RuleFor(x => x.Email)
            .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.Email), ApplyConditionTo.CurrentValidator)
                .WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter.");
    }
}
