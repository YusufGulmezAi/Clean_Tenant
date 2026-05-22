using FluentValidation;
using Microsoft.Extensions.Localization;

namespace CleanTenant.Application.Features.Main.Budgeting.UnitParticipationGroups;

/// <summary><see cref="AddUnitToGroupCommand"/> FluentValidation kuralları.</summary>
public sealed class AddUnitToGroupCommandValidator : AbstractValidator<AddUnitToGroupCommand>
{
    /// <summary>DI bağımlılıklarını alır.</summary>
    public AddUnitToGroupCommandValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.ParticipationGroupId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
