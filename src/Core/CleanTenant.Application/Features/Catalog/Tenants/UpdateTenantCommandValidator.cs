using CleanTenant.Domain.Identity.Tenants;
using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.Tenants;

/// <summary>
/// <see cref="UpdateTenantCommand"/> validation kuralları. CreateTenant ile aynı
/// format kontrolleri; Admin User alanları yok (Update'te zaten kullanıcı atanmış).
/// </summary>
public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    /// <summary>Validation kurallarını tanımlar.</summary>
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Yönetim id zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yönetim adı zorunlu.")
            .MaximumLength(256);

        RuleFor(x => x.LegalName)
            .MaximumLength(512);

        RuleFor(x => x.Address)
            .MaximumLength(512);

        RuleFor(x => x.LegalIdentityType)
            .IsInEnum().WithMessage("Geçerli bir kimlik tipi seçin (VKN/TCKN/YKN).");

        RuleFor(x => x.LegalIdentityNumber)
            .NotEmpty().WithMessage("Kimlik numarası zorunlu.")
            .Matches(@"^[1-9][0-9]{9}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Vkn, ApplyConditionTo.CurrentValidator)
                .WithMessage("VKN 10 haneli, ilk hane 1-9 arasında olmalı.")
            .Matches(@"^[1-9][0-9]{10}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Tckn, ApplyConditionTo.CurrentValidator)
                .WithMessage("TCKN 11 haneli, ilk hane 1-9 arasında olmalı.")
            .Matches(@"^99[0-9]{9}$")
                .When(x => x.LegalIdentityType == LegalIdentityType.Ykn, ApplyConditionTo.CurrentValidator)
                .WithMessage("YKN 11 haneli, '99' ile başlamalı.");

        RuleFor(x => x.BillingTier)
            .IsInEnum().WithMessage("Geçerli bir katman seçin.");
    }
}
