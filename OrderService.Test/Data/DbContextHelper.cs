using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;

namespace OrderService.Test.Data
{
    public static class DbContextHelper
    {
        public static (OrderDbContext Context, SqliteConnection Connection) GetSqliteInMemoryDbContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new OrderDbContext(options);
            context.Database.EnsureCreated();

            return (context, connection);
        }
    }
}
