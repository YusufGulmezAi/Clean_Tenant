using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.BuildingTypes;

internal sealed class UpdateBuildingTypeCommandValidator : AbstractValidator<UpdateBuildingTypeCommand>
{
    public UpdateBuildingTypeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Yapı tipi ID'si zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Yapı tipi adı zorunlu.")
            .MaximumLength(100).WithMessage("Yapı tipi adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.Description)
            .MaximumLength(250).WithMessage("Açıklama maksimum 250 karakter olmalı.");
    }
}
