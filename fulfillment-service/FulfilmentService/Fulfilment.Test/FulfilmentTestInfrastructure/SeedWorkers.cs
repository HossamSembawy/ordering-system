using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fulfilment.Test.FulfilmentTestInfrastructure
{
    public static class SeedWorkers
    {
        public static void Seed(SqliteInMemoryDb db)
        {
            var workers = new List<FulfilmentService.Models.Worker>
            {
                new FulfilmentService.Models.Worker { Name = "Worker 1" ,ActiveTasksCount=0},
                new FulfilmentService.Models.Worker { Name = "Worker 2",ActiveTasksCount=0 },
                new FulfilmentService.Models.Worker { Name = "Worker 3" ,ActiveTasksCount=0},
                new FulfilmentService.Models.Worker { Name = "Worker 4" ,ActiveTasksCount=0},
                new FulfilmentService.Models.Worker { Name = "Worker 5" ,ActiveTasksCount=0}
            };

            db.Context.Workers.AddRange(workers);
            db.Context.SaveChanges();
        }
    }
}
