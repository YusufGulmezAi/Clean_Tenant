using FluentValidation;

namespace CleanTenant.Application.Features.Main.Companies;

/// <summary>
/// <see cref="CreateCompanyCommand"/> validation kuralları.
/// VKN formatı (10 hane) DB CHECK constraint'iyle de zorlanır;
/// FluentValidation kullanıcıya erken + Türkçe geri bildirim sağlar.
/// </summary>
public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Yönetim (Tenant) kimliği zorunlu.");

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
