
using FulfilmentService.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public sealed class SqliteInMemoryDb : IDisposable
{
    private readonly SqliteConnection _connection;
    public FulfilmentDbContext Context { get; }

    public SqliteInMemoryDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FulfilmentDbContext>()
            .UseSqlite(_connection)
        .Options;

        Context = new FulfilmentDbContext(options);

        // Create schema (or run migrations)
        Context.Database.EnsureCreated();
        // If you use migrations, prefer:
        // Context.Database.Migrate();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
