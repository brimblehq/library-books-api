namespace LibraryBooks.Api.Books;

public class BookRequest
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Isbn { get; set; } = "";
    public int PublishedYear { get; set; }
    public int CopiesAvailable { get; set; }
}
