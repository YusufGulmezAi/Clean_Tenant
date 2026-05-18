using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Districts;

internal sealed class UpdateDistrictCommandValidator : AbstractValidator<UpdateDistrictCommand>
{
    public UpdateDistrictCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("İlçe ID'si zorunlu.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İlçe adı zorunlu.")
            .MaximumLength(100).WithMessage("İlçe adı maksimum 100 karakter olmalı.");

        RuleFor(x => x.ProvinceId)
            .NotEmpty().WithMessage("İl ID'si zorunlu.");
    }
}
