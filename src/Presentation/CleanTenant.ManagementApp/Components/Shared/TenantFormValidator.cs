using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <para>
/// <see cref="TenantForm"/>'un client-side validator'ı. <see cref="TenantFormMode"/>
/// parametresine göre koşullu kural ekler:
/// </para>
/// <list type="bullet">
///   <item>Tümünde: <c>Name</c> + <c>LegalName</c>/<c>Address</c> max length.</item>
///   <item>Create/Edit: kimlik tipi+numarası + BillingTier.</item>
///   <item>Create: Sorumlu Yönetici alanları (ad/soyad/e-posta/telefon).</item>
/// </list>
/// <para>
/// Sunucu tarafı authoritative kontrol için
/// <c>CreateTenantCommandValidator</c> / <c>UpdateTenantCommandValidator</c>
/// çalışır; bu validator yalnız erken UX geri bildirimi sağlar.
/// </para>
/// </summary>
public sealed class TenantFormValidator : AbstractValidator<TenantFormModel>
{
    /// <summary>Belirli moda göre kuralları kurar.</summary>
    public TenantFormValidator(TenantFormMode mode)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yönetim adı zorunlu.")
            .MaximumLength(256).WithMessage("Yönetim adı en fazla 256 karakter.");

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage("Yasal ad en fazla 512 karakter.");

        RuleFor(x => x.Address)
            .MaximumLength(512).WithMessage("Adres en fazla 512 karakter.");

        if (mode != TenantFormMode.Settings)
        {
            RuleFor(x => x.LegalIdentityType)
                .IsInEnum().WithMessage("Geçerli bir kimlik tipi seçin (VKN/TCKN/YKN).");

            RuleFor(x => x.LegalIdentityNumber)
                .NotEmpty().WithMessage("Kimlik numarası zorunlu.")
                .Matches(@"^[0-9]{10}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Vkn, ApplyConditionTo.CurrentValidator)
                    .WithMessage("VKN 10 haneli rakamlardan oluşmalı.")
                .Matches(@"^[1-9][0-9]{10}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Tckn, ApplyConditionTo.CurrentValidator)
                    .WithMessage("TCKN 11 haneli, ilk hane 1-9 arasında olmalı.")
                .Matches(@"^99[0-9]{9}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Ykn, ApplyConditionTo.CurrentValidator)
                    .WithMessage("YKN 11 haneli, '99' ile başlamalı.");

            RuleFor(x => x.BillingTier)
                .IsInEnum().WithMessage("Geçerli bir katman seçin.");
        }

        if (mode == TenantFormMode.Create)
        {
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
}
