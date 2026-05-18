using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class UpdateNeighborhoodCommandValidator : AbstractValidator<UpdateNeighborhoodCommand>
{
    public UpdateNeighborhoodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Mahalle ID'si zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mahalle adı zorunlu.")
            .MaximumLength(100).WithMessage("Mahalle adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.DistrictId)
            .NotEmpty().WithMessage("İlçe ID'si zorunlu.");
    }
}
