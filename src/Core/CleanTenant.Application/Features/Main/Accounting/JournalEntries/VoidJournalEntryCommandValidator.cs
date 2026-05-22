using FluentValidation;

namespace CleanTenant.Application.Features.Main.Accounting.JournalEntries;

/// <summary>
/// <see cref="VoidJournalEntryCommand"/> FluentValidation doğrulayıcısı.
/// </summary>
public sealed class VoidJournalEntryCommandValidator
    : AbstractValidator<VoidJournalEntryCommand>
{
    /// <summary>Doğrulama kurallarını tanımlar.</summary>
    public VoidJournalEntryCommandValidator()
    {
        RuleFor(x => x.VoidReason)
            .NotEmpty()
            .WithErrorCode("ACC-305")
            .WithMessage("İptal gerekçesi zorunludur.")
            .MaximumLength(500)
            .WithErrorCode("ACC-305")
            .WithMessage("İptal gerekçesi en fazla 500 karakter olabilir.");
    }
}
