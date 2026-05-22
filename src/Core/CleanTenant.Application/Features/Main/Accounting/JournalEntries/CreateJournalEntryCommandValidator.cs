using FluentValidation;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="CreateJournalEntryCommand"/> FluentValidation doğrulayıcısı.
/// <para>
/// Temel format kontrolleri burada yapılır. Veritabanı bağımlı kontroller
/// (hesap kodu varlığı, dönem durumu vb.) handler'da uygulanır.
/// </para>
/// </summary>
public sealed class CreateJournalEntryCommandValidator
    : AbstractValidator<CreateJournalEntryCommand>
{
    /// <summary>Doğrulama kurallarını tanımlar.</summary>
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Açıklama zorunludur.")
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");

        RuleFor(x => x.EntryDate)
            .NotEmpty().WithMessage("Fiş tarihi zorunludur.");

        RuleFor(x => x.AccountingPeriodId)
            .NotEmpty().WithMessage("Muhasebe dönemi zorunludur.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("ACC-101")
            .WithMessage("Yevmiye fişi en az 2 satır içermelidir.")
            .Must(l => l.Count >= 2)
            .WithErrorCode("ACC-101")
            .WithMessage("Yevmiye fişi en az 2 satır içermelidir.");

        RuleFor(x => x.Lines)
            .Must(l => l.Sum(i => i.Debit) == l.Sum(i => i.Credit))
            .WithErrorCode("ACC-103")
            .WithMessage("Borç ve alacak toplamları eşit olmalıdır.")
            .Must(l => l.Sum(i => i.Debit) > 0)
            .WithErrorCode("ACC-103")
            .WithMessage("Toplam borç sıfırdan büyük olmalıdır.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountCodeId)
                .NotEmpty().WithMessage("Hesap kodu zorunludur.");

            line.RuleFor(l => l.Debit)
                .GreaterThanOrEqualTo(0).WithMessage("Borç tutarı negatif olamaz.");

            line.RuleFor(l => l.Credit)
                .GreaterThanOrEqualTo(0).WithMessage("Alacak tutarı negatif olamaz.");

            line.RuleFor(l => l)
                .Must(l => (l.Debit == 0 || l.Credit == 0) && (l.Debit > 0 || l.Credit > 0))
                .WithErrorCode("ACC-105")
                .WithMessage("Her satır yalnızca borç veya alacak içerebilir, ikisi birden olamaz.");
        });
    }
}
