using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Provinces;

internal sealed class UpdateProvinceCommandValidator : AbstractValidator<UpdateProvinceCommand>
{
    public UpdateProvinceCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("İl ID'si zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İl adı zorunlu.")
            .MaximumLength(100).WithMessage("İl adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.PlateCode)
            .InclusiveBetween(1, 81)
            .When(x => x.PlateCode.HasValue)
            .WithMessage("Plaka kodu 1-81 arasında olmalı.");
    }
}
