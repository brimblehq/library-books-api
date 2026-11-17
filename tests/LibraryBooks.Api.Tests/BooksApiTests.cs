using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LibraryBooks.Api.Books;

namespace LibraryBooks.Api.Tests;

public class BooksApiTests : IDisposable
{
    readonly string databasePath = LibraryApiFactory.NewDatabasePath();
    readonly LibraryApiFactory factory;
    readonly HttpClient client;

    public BooksApiTests()
    {
        factory = new LibraryApiFactory(databasePath);
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
        LibraryApiFactory.DeleteDatabase(databasePath);
    }

    static BookRequest NewBook(string title = "Things Fall Apart", string author = "Chinua Achebe", string isbn = "9780385474542") => new()
    {
        Title = title,
        Author = author,
        Isbn = isbn,
        PublishedYear = 1958,
        CopiesAvailable = 3,
    };

    async Task<Book> Create(BookRequest request)
    {
        var response = await client.PostAsJsonAsync("/books", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Book>())!;
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Lists_no_books_on_a_fresh_database()
    {
        var books = await client.GetFromJsonAsync<List<Book>>("/books");

        Assert.Empty(books!);
    }

    [Fact]
    public async Task Creates_a_book_and_returns_its_location()
    {
        var response = await client.PostAsJsonAsync("/books", NewBook());
        var book = await response.Content.ReadFromJsonAsync<Book>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/books/{book!.Id}", response.Headers.Location!.ToString());
        Assert.Equal("Things Fall Apart", book.Title);
        Assert.Equal(3, book.CopiesAvailable);
    }

    [Fact]
    public async Task Gets_a_book_by_id()
    {
        var created = await Create(NewBook());

        var book = await client.GetFromJsonAsync<Book>($"/books/{created.Id}");

        Assert.Equal(created.Id, book!.Id);
        Assert.Equal("Chinua Achebe", book.Author);
        Assert.Equal(DateTimeKind.Utc, book.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, book.UpdatedAt.Kind);
    }

    [Fact]
    public async Task Returns_404_for_a_missing_book()
    {
        var response = await client.GetAsync("/books/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Searches_by_title_or_author_case_insensitively()
    {
        await Create(NewBook());
        await Create(NewBook("Half of a Yellow Sun", "Chimamanda Ngozi Adichie", "9781400095209"));
        await Create(NewBook("Purple Hibiscus", "Chimamanda Ngozi Adichie", "9781616202415"));

        var byAuthor = await client.GetFromJsonAsync<List<Book>>("/books?search=adichie");
        var byTitle = await client.GetFromJsonAsync<List<Book>>("/books?search=FALL");

        Assert.Equal(["Half of a Yellow Sun", "Purple Hibiscus"], byAuthor!.Select(book => book.Title));
        Assert.Equal("Things Fall Apart", Assert.Single(byTitle!).Title);
    }

    [Fact]
    public async Task Updates_a_book()
    {
        var created = await Create(NewBook());
        var update = NewBook();
        update.CopiesAvailable = 10;

        var response = await client.PutAsJsonAsync($"/books/{created.Id}", update);
        var fetched = await client.GetFromJsonAsync<Book>($"/books/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(10, fetched!.CopiesAvailable);
        Assert.True(fetched.UpdatedAt >= created.UpdatedAt);
    }

    [Fact]
    public async Task Returns_404_when_updating_a_missing_book()
    {
        var response = await client.PutAsJsonAsync("/books/999", NewBook());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deletes_a_book()
    {
        var created = await Create(NewBook());

        var response = await client.DeleteAsync($"/books/{created.Id}");
        var afterDelete = await client.GetAsync($"/books/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Returns_404_when_deleting_a_missing_book()
    {
        var response = await client.DeleteAsync("/books/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_a_duplicate_isbn_on_create()
    {
        await Create(NewBook());

        var response = await client.PostAsJsonAsync("/books", NewBook("Another Title"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_taking_another_books_isbn_on_update()
    {
        await Create(NewBook());
        var second = await Create(NewBook("Arrow of God", isbn: "9780385014809"));

        var response = await client.PutAsJsonAsync($"/books/{second.Id}", NewBook("Arrow of God"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    public static TheoryData<BookRequest, string> InvalidBooks()
    {
        var nextYear = DateTime.UtcNow.Year + 1;
        return new()
        {
            { With(book => book.Title = ""), "Title" },
            { With(book => book.Title = "   "), "Title" },
            { With(book => book.Title = new string('a', 201)), "Title" },
            { With(book => book.Author = "   "), "Author" },
            { With(book => book.Isbn = "12-34"), "Isbn" },
            { With(book => book.Isbn = "9780385474543"), "Isbn" },
            { With(book => book.Isbn = "0306406153"), "Isbn" },
            { With(book => book.PublishedYear = 1200), "PublishedYear" },
            { With(book => book.PublishedYear = nextYear), "PublishedYear" },
            { With(book => book.CopiesAvailable = -1), "CopiesAvailable" },
        };
    }

    static BookRequest With(Action<BookRequest> change)
    {
        var request = NewBook();
        change(request);
        return request;
    }

    [Theory]
    [MemberData(nameof(InvalidBooks))]
    public async Task Rejects_invalid_books_with_field_errors(BookRequest request, string invalidField)
    {
        var response = await client.PostAsJsonAsync("/books", request);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty(invalidField, out _), problem.ToString());
        Assert.Empty((await client.GetFromJsonAsync<List<Book>>("/books"))!);
    }

    [Fact]
    public async Task Rejects_an_empty_payload_with_every_required_field()
    {
        var response = await client.PostAsync("/books", JsonContent.Create(new { }));
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.All(new[] { "Title", "Author", "Isbn", "PublishedYear" }, field => Assert.True(errors.TryGetProperty(field, out _), field));
    }

    [Fact]
    public async Task Rejects_an_invalid_update_without_changing_the_book()
    {
        var created = await Create(NewBook());

        var response = await client.PutAsJsonAsync($"/books/{created.Id}", With(book => book.Title = ""));
        var book = await client.GetFromJsonAsync<Book>($"/books/{created.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Things Fall Apart", book!.Title);
    }

    [Fact]
    public async Task Accepts_an_isbn10_ending_in_x()
    {
        await Create(With(book => book.Isbn = "043942089X"));
    }

    [Fact]
    public async Task Keeps_books_in_sqlite_across_restarts()
    {
        var created = await Create(NewBook());
        client.Dispose();
        factory.Dispose();

        using var restarted = new LibraryApiFactory(databasePath);
        using var restartedClient = restarted.CreateClient();
        var book = await restartedClient.GetFromJsonAsync<Book>($"/books/{created.Id}");

        Assert.Equal("Things Fall Apart", book!.Title);
    }
}
