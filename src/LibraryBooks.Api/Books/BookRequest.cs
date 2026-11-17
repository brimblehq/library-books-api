using System.ComponentModel.DataAnnotations;

namespace LibraryBooks.Api.Books;

public class BookRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; } = "";

    [Required, StringLength(120)]
    public string Author { get; set; } = "";

    [Required, RegularExpression(@"^(\d{9}[\dX]|\d{13})$", ErrorMessage = "ISBN must be 10 or 13 characters, digits only (ISBN-10 may end in X).")]
    public string Isbn { get; set; } = "";

    [Range(1450, 2100)]
    public int PublishedYear { get; set; }

    [Range(0, 10_000)]
    public int CopiesAvailable { get; set; }
}
