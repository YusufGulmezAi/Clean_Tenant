using FluentValidation;

namespace CleanTenant.Application.Features.Catalog.LookUp.Banks;

internal sealed class UpdateBankCommandValidator : AbstractValidator<UpdateBankCommand>
{
    public UpdateBankCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Banka ID'si zorunlu.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Banka tam adı zorunlu.")
            .MaximumLength(200).WithMessage("Banka tam adı maksimum 200 karakter olmalı.");

        RuleFor(x => x.ShortName)
            .NotEmpty().WithMessage("Banka kısa adı zorunlu.")
            .MaximumLength(30).WithMessage("Banka kısa adı maksimum 30 karakter olmalı.");

        RuleFor(x => x.EftCode)
            .MaximumLength(10).WithMessage("EFT kodu maksimum 10 karakter olmalı.");
    }
}
