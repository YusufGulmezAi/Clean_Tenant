using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class CreateDistrictCommandValidator : AbstractValidator<CreateDistrictCommand>
{
    public CreateDistrictCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İlçe adı zorunlu.")
            .MaximumLength(100).WithMessage("İlçe adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.ProvinceId)
            .NotEmpty().WithMessage("İl ID'si zorunlu.");
    }
}
