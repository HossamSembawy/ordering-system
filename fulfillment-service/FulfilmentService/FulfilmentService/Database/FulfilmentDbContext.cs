using FulfilmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace FulfilmentService.Database
{
    public class FulfilmentDbContext(DbContextOptions<FulfilmentDbContext> options) : DbContext(options)
    {
        public DbSet<Worker> Workers { get; set; }
        public DbSet<FulfilmentTask> FulfilmentTasks { get; set; }
        public DbSet<Cursor> Cursors { get; set; }
    }
}
