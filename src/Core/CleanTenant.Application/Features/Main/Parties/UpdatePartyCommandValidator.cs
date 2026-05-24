using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Parties;

/// <summary><see cref="UpdatePartyCommand"/> FluentValidation kuralları.</summary>
public sealed class UpdatePartyCommandValidator : AbstractValidator<UpdatePartyCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public UpdatePartyCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.TradeName).MaximumLength(200);
        RuleFor(x => x.Tckn).Length(11).When(x => !string.IsNullOrWhiteSpace(x.Tckn));
        RuleFor(x => x.Vkn).Length(10).When(x => !string.IsNullOrWhiteSpace(x.Vkn));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.AddressLine).MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.KvkkConsentChannel).MaximumLength(50);
    }
}
