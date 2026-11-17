using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace LibraryBooks.Api.Books;

public static class BookEndpoints
{
    public static void MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/books");

        books.MapGet("/", ListBooks);
        books.MapGet("/{id:int}", GetBook);
        books.MapPost("/", CreateBook);
        books.MapPut("/{id:int}", UpdateBook);
        books.MapDelete("/{id:int}", DeleteBook);
    }

    static async Task<Ok<List<Book>>> ListBooks(LibraryDbContext db, string? search)
    {
        var query = db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(book => EF.Functions.Like(book.Title, pattern) || EF.Functions.Like(book.Author, pattern));
        }

        return TypedResults.Ok(await query.OrderBy(book => book.Title).ToListAsync());
    }

    static async Task<Results<Ok<Book>, NotFound>> GetBook(int id, LibraryDbContext db)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(book => book.Id == id);
        if (book is null) return TypedResults.NotFound();
        return TypedResults.Ok(book);
    }

    static async Task<Results<Created<Book>, Conflict<ProblemDetailsBody>>> CreateBook(BookRequest request, LibraryDbContext db)
    {
        if (await db.Books.AnyAsync(book => book.Isbn == request.Isbn))
            return DuplicateIsbn(request.Isbn);

        var now = DateTime.UtcNow;
        var book = new Book
        {
            Title = request.Title.Trim(),
            Author = request.Author.Trim(),
            Isbn = request.Isbn,
            PublishedYear = request.PublishedYear,
            CopiesAvailable = request.CopiesAvailable,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Books.Add(book);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/books/{book.Id}", book);
    }

    static async Task<Results<Ok<Book>, NotFound, Conflict<ProblemDetailsBody>>> UpdateBook(int id, BookRequest request, LibraryDbContext db)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return TypedResults.NotFound();

        if (await db.Books.AnyAsync(other => other.Isbn == request.Isbn && other.Id != id))
            return DuplicateIsbn(request.Isbn);

        book.Title = request.Title.Trim();
        book.Author = request.Author.Trim();
        book.Isbn = request.Isbn;
        book.PublishedYear = request.PublishedYear;
        book.CopiesAvailable = request.CopiesAvailable;
        book.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok(book);
    }

    static async Task<Results<NoContent, NotFound>> DeleteBook(int id, LibraryDbContext db)
    {
        var deleted = await db.Books.Where(book => book.Id == id).ExecuteDeleteAsync();
        if (deleted == 0) return TypedResults.NotFound();
        return TypedResults.NoContent();
    }

    static Conflict<ProblemDetailsBody> DuplicateIsbn(string isbn) =>
        TypedResults.Conflict(new ProblemDetailsBody("Duplicate ISBN", $"A book with ISBN {isbn} already exists."));
}

public record ProblemDetailsBody(string Title, string Detail);
