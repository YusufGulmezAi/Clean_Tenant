using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.ParticipationGroups;

/// <summary><see cref="CreateParticipationGroupCommand"/> FluentValidation kuralları.</summary>
public sealed class CreateParticipationGroupCommandValidator : AbstractValidator<CreateParticipationGroupCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public CreateParticipationGroupCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
