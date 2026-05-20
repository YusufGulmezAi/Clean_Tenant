using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <see cref="CreateTenantCommand"/> validation kuralları. Format kontrolleri
/// (regex) DB CHECK constraint'iyle de zorlanır; FluentValidation kullanıcıya
/// erken + lokalize geri bildirim sağlar (v0.2.11.d — DbStringLocalizer).
/// </summary>
public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateTenantCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => localizer["Validation.Tenant.Name.Required"].Value)
            .MaximumLength(256).WithMessage(_ => localizer["Validation.Tenant.Name.MaxLength", 256].Value);

        RuleFor(x => x.LegalName)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.Tenant.LegalName.MaxLength", 512].Value);

        RuleFor(x => x.Address)
            .MaximumLength(512).WithMessage(_ => localizer["Validation.Tenant.Address.MaxLength", 512].Value);

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

        // Sorumlu Yönetici
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
}
