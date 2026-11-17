using FluentValidation;

namespace LibraryBooks.Api.Books;

public class BookRequestValidator : AbstractValidator<BookRequest>
{
    public BookRequestValidator(TimeProvider clock)
    {
        RuleFor(book => book.Title).NotEmpty().MaximumLength(200);

        RuleFor(book => book.Author).NotEmpty().MaximumLength(120);

        RuleFor(book => book.Isbn)
            .NotEmpty()
            .Must(Isbn.IsValid)
            .WithMessage("ISBN must be a valid ISBN-10 or ISBN-13, digits only (ISBN-10 may end in X).");

        RuleFor(book => book.PublishedYear)
            .InclusiveBetween(1450, clock.GetUtcNow().Year)
            .WithMessage(book => $"Published year must be between 1450 and {clock.GetUtcNow().Year}.");

        RuleFor(book => book.CopiesAvailable).InclusiveBetween(0, 10_000);
    }
}
