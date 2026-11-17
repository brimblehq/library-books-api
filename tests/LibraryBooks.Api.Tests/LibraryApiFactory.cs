using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace LibraryBooks.Api.Tests;

public class LibraryApiFactory(string databasePath) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Library", $"Data Source={databasePath}");
    }

    public static string NewDatabasePath() => Path.Combine(Path.GetTempPath(), $"library-tests-{Guid.NewGuid():N}.db");

    public static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-shm", "-wal" })
            File.Delete(databasePath + suffix);
    }
}
