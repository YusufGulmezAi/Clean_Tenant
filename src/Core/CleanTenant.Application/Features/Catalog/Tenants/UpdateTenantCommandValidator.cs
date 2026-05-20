using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <see cref="UpdateTenantCommand"/> validation kuralları. CreateTenant ile aynı
/// format kontrolleri; Admin User alanları yok (Update'te zaten kullanıcı atanmış).
/// v0.2.11.d — lokalize.
/// </summary>
public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdateTenantCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(_ => localizer["Validation.Tenant.Id.Required"].Value);

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
    }
}
