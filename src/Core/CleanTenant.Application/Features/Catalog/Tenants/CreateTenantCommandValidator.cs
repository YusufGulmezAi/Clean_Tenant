using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <see cref="CreateTenantCommand"/> validation kuralları. Format kontrolleri
/// (regex) DB CHECK constraint'iyle de zorlanır; FluentValidation kullanıcıya
/// erken + Türkçe geri bildirim sağlar.
/// </summary>
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yönetim adı zorunlu.")
            .MaximumLength(256).WithMessage("Yönetim adı en fazla 256 karakter.");

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage("Yasal ad en fazla 512 karakter.");

        RuleFor(x => x.Address)
            .MaximumLength(512).WithMessage("Adres en fazla 512 karakter.");

        RuleFor(x => x.LegalIdentityType)
            .IsInEnum().WithMessage("Geçerli bir kimlik tipi seçin (VKN/TCKN/YKN).");

        RuleFor(x => x.LegalIdentityNumber)
            .NotEmpty().WithMessage("Kimlik numarası zorunlu.")
            .Matches(@"^[1-9][0-9]{9}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Vkn)
                .WithMessage("VKN 10 haneli, ilk hane 1-9 arasında olmalı.")
            .Matches(@"^[1-9][0-9]{10}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Tckn)
                .WithMessage("TCKN 11 haneli, ilk hane 1-9 arasında olmalı.")
            .Matches(@"^99[0-9]{9}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Ykn)
                .WithMessage("YKN 11 haneli, '99' ile başlamalı.");

        RuleFor(x => x.BillingTier)
            .IsInEnum().WithMessage("Geçerli bir katman seçin.");

        // Sorumlu Yönetici
        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("Sorumlu Yönetici adı zorunlu.")
            .MaximumLength(100);

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Sorumlu Yönetici soyadı zorunlu.")
            .MaximumLength(100);

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Sorumlu Yönetici e-postası zorunlu.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.AdminPhone)
            .NotEmpty().WithMessage("Sorumlu Yönetici cep telefonu zorunlu.")
            .Matches(@"^0\(5\d{2}\) \d{3}-\d{2}-\d{2}$")
            .WithMessage("Cep telefonu formatı: 0(5XX) XXX-XX-XX");
    }
}
