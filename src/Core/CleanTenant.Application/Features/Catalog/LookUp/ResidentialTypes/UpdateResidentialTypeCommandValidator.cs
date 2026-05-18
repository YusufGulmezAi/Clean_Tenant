using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.ResidentialTypes;

internal sealed class UpdateResidentialTypeCommandValidator : AbstractValidator<UpdateResidentialTypeCommand>
{
    public UpdateResidentialTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mesken tipi ID'si zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mesken tipi adı zorunlu.")
            .MaximumLength(15).WithMessage("Mesken tipi adı maksimum 15 karakter olmalı.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Açıklama maksimum 250 karakter olmalı.");
    }
}
