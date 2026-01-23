using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Test.Data
{
    public static class TestDbFactory
    {
        public static OrderDbContext CreateContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseSqlite(connection)
                .Options;

            return new OrderDbContext(options);
        }
    }

}
