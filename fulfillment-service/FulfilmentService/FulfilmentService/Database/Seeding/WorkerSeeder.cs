using FulfilmentService.Models;

namespace FulfilmentService.Database.Seeding
{
    public static class WorkerSeeder
    {
        public static void SeedWorkers(FulfilmentDbContext dbContext)
        {
            if (!dbContext.Workers.Any())
            {
                for (int i = 1; i <= 5; i++)
                {
                    dbContext.Workers.Add(new Worker
                    {
                        Name = $"Worker {i}",
                        ActiveTasksCount = 0
                    });
                }
                dbContext.SaveChanges();
            }
        }
    }
}
