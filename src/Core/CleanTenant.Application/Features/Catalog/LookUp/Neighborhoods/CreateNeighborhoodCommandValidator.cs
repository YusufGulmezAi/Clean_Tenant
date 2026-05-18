using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Neighborhoods;

internal sealed class CreateNeighborhoodCommandValidator : AbstractValidator<CreateNeighborhoodCommand>
{
    public CreateNeighborhoodCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mahalle adı zorunlu.")
            .MaximumLength(100).WithMessage("Mahalle adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.DistrictId)
            .NotEmpty().WithMessage("İlçe ID'si zorunlu.");
    }
}
