using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Test.Data
{
    public static class DbContextHelper
    {
        public static OrderDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated DB per test
                .Options;

            return new OrderDbContext(options);
        }
    }
}
