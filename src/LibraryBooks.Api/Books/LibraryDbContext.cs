using Microsoft.EntityFrameworkCore;

namespace LibraryBooks.Api.Books;

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var book = modelBuilder.Entity<Book>();
        book.HasIndex(entity => entity.Isbn).IsUnique();
        book.Property(entity => entity.CreatedAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        book.Property(entity => entity.UpdatedAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
