using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.ManagementApp.Components.Shared;

/// <summary>
/// <para>
/// <see cref="TenantForm"/>'un client-side validator'ı. v0.2.11.d — lokalize
/// (DbStringLocalizer); <see cref="TenantFormMode"/> parametresine göre
/// koşullu kural ekler.
/// </para>
/// <para>
/// Sunucu tarafı authoritative kontrol için
/// <c>CreateTenantCommandValidator</c> / <c>UpdateTenantCommandValidator</c>
/// çalışır; bu validator yalnız erken UX geri bildirimi sağlar.
/// </para>
/// </summary>
public sealed class TenantFormValidator : AbstractValidator<TenantFormModel>
{
    /// <summary>Belirli moda ve localizer'a göre kuralları kurar.</summary>
    public TenantFormValidator(TenantFormMode mode, IStringLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.Tenant.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.Tenant.Name.MaxLength", 256].Value);

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.Tenant.LegalName.MaxLength", 512].Value);

        RuleFor(x => x.Address)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.Tenant.Address.MaxLength", 512].Value);

        if (mode != TenantFormMode.Settings)
        {
            RuleFor(x => x.LegalIdentityType)
                .IsInEnum().WithMessage(_ => localizer["Validation.Tenant.IdentityType.Invalid"].Value);

            RuleFor(x => x.LegalIdentityNumber)
                .NotEmpty().WithMessage(_ => localizer["Validation.Tenant.IdentityNumber.Required"].Value)
                .Matches(@"^[0-9]{10}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Vkn, ApplyConditionTo.CurrentValidator)
                    .WithMessage(_ => localizer["Validation.Tenant.Vkn.Format"].Value)
                .Matches(@"^[1-9][0-9]{10}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Tckn, ApplyConditionTo.CurrentValidator)
                    .WithMessage(_ => localizer["Validation.Tenant.Tckn.Format"].Value)
                .Matches(@"^99[0-9]{9}$")
                    .When(x => x.LegalIdentityType == LegalIdentityType.Ykn, ApplyConditionTo.CurrentValidator)
                    .WithMessage(_ => localizer["Validation.Tenant.Ykn.Format"].Value);

            RuleFor(x => x.BillingTier)
                .IsInEnum().WithMessage(_ => localizer["Validation.Tenant.BillingTier.Invalid"].Value);
        }

        if (mode == TenantFormMode.Create)
        {
            RuleFor(x => x.AdminFirstName)
                .NotEmpty().WithMessage(_ => localizer["Validation.TenantAdmin.FirstName.Required"].Value)
                .MaximumLength(100);

            RuleFor(x => x.AdminLastName)
                .NotEmpty().WithMessage(_ => localizer["Validation.TenantAdmin.LastName.Required"].Value)
                .MaximumLength(100);

            RuleFor(x => x.AdminEmail)
                .NotEmpty().WithMessage(_ => localizer["Validation.TenantAdmin.Email.Required"].Value)
                .EmailAddress().WithMessage(_ => localizer["Validation.Tenant.Contact.Email.Invalid"].Value)
                .MaximumLength(256);

            RuleFor(x => x.AdminPhone)
                .NotEmpty().WithMessage(_ => localizer["Validation.TenantAdmin.Phone.Required"].Value)
                .Matches(@"^0\(5\d{2}\) \d{3}-\d{2}-\d{2}$")
                .WithMessage(_ => localizer["Validation.TenantAdmin.Phone.Format"].Value);
        }

        // v0.2.11.d — İletişim ve Sözleşme alanları (tüm modlarda opsiyonel)
        RuleFor(x => x.ContactPerson).MaximumLength(200);
        RuleFor(x => x.ContactEmail)
            .EmailAddress()
                .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail), ApplyConditionTo.CurrentValidator)
                .WithMessage(_ => localizer["Validation.Tenant.Contact.Email.Invalid"].Value)
            .MaximumLength(256);
        RuleFor(x => x.ContactPhone).MaximumLength(32);

        RuleFor(x => x.ContractEndDate)
            .GreaterThanOrEqualTo(x => x.ContractStartDate)
                .When(x => x.ContractStartDate.HasValue && x.ContractEndDate.HasValue)
                .WithMessage(_ => localizer["Validation.Tenant.ContractEnd.AfterStart"].Value);

        RuleFor(x => x.TransitionGraceDays)
            .GreaterThanOrEqualTo(0)
                .When(x => x.TransitionGraceDays.HasValue)
                .WithMessage(_ => localizer["Validation.Tenant.GraceDays.NonNegative"].Value);
    }
}
