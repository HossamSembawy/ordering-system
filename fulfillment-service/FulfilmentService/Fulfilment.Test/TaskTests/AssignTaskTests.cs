using Fulfilment.Test.FulfilmentTestInfrastructure;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Repositories;

namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class AssignTaskTests
    {
        private SqliteInMemoryDb _db = default!;
        private IFulfilmentTaskRepository _repo = default!;
        private IWorkerRepository _workerRepository = default!;

        [SetUp]
        public void SetUp()
        {

            _db = new SqliteInMemoryDb();


            if (!_db.Context.Cursors.Any())
            {
                _db.Context.Cursors.Add(new Cursor { Id = 1, Current = 0 });
                _db.Context.SaveChanges();
            }

            _workerRepository = new WorkerRepository(_db.Context);
            _repo = new FulfilmentTaskRepository(_db.Context, _workerRepository);
            SeedWorkers.Seed(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }
        [Test]
        public void AssignTask_WhenTaskNotFound_Throws()
        {
            var ex = Assert.ThrowsAsync<Exception>(async () => await _repo.AssignTask(999));
            Assert.That(ex!.Message, Is.EqualTo("Task not found"));
        }


        [Test]
        public async Task AssignTask_WhenTaskAlreadyAssigned_Throws()
        {
            // seed assigned task
            var task = new FulfillmentTask { OrderId = 1, WorkerId = 2, Status = "Assigned" };
            _db.Context.FulfilmentTasks.Add(task);
            await _db.Context.SaveChangesAsync();

            var ex = Assert.ThrowsAsync<Exception>(async () => await _repo.AssignTask(task.Id));
            Assert.That(ex!.Message, Is.EqualTo("Task already assigned"));
        }


        [Test]
        public async Task AssignTask_AssignsBasedOnCursor_WhenWorkerHasCapacity()
        {
            await SeedCursor(current: 0);
            int taskId = await SeedUnassignedTask(orderId: 123);

            // Make worker 1 available (0 active tasks)
            await SeedActiveTasksForWorker(workerId: 1, count: 0);

            var response = await _repo.AssignTask(taskId);

            Assert.That(response.Id, Is.EqualTo(taskId));
            Assert.That(response.OrderId, Is.EqualTo(123));
            Assert.That(response.Status, Is.EqualTo("Assigned"));

            // Verify DB persisted assignment
            var persisted = await _db.Context.FulfilmentTasks.FindAsync(taskId);
            Assert.That(persisted!.WorkerId, Is.EqualTo(1));
            Assert.That(persisted.Status, Is.EqualTo("Assigned"));

            var cursor = _db.Context.Cursors.First();
            Assert.That(cursor.Current, Is.EqualTo(1));
        }



        [Test]
        public async Task AssignTask_SkipsWorkersAtCapacity_AndAssignsNextAvailable()
        {
            await SeedCursor(current: 0);
            int taskId = await SeedUnassignedTask(orderId: 456);

            await SeedActiveTasksForWorker(1, 5);

            await SeedActiveTasksForWorker(2, 5);

            await SeedActiveTasksForWorker(3, 4);

            var response = await _repo.AssignTask(taskId);

            var persisted = await _db.Context.FulfilmentTasks.FindAsync(taskId);
            Assert.That(persisted!.WorkerId, Is.EqualTo(3));
            Assert.That(persisted.Status, Is.EqualTo("Assigned"));

            var cursor = _db.Context.Cursors.First();
            Assert.That(cursor.Current, Is.EqualTo(3));
        }



        [Test]
        public async Task AssignTask_WhenAllWorkersAtCapacity_ThrowsNoAvailableWorkers()
        {
            await SeedCursor(current: 0);
            int taskId = await SeedUnassignedTask(orderId: 789);

            // All 5 workers have 5 active tasks
            for (int w = 1; w <= 5; w++)
                await SeedActiveTasksForWorker(w, 5);

            var ex = Assert.ThrowsAsync<Exception>(async () => await _repo.AssignTask(taskId));
            Assert.That(ex!.Message, Is.EqualTo("No available workers"));

            // Verify task is still unassigned
            var task = await _db.Context.FulfilmentTasks.FindAsync(taskId);
            Assert.That(task!.WorkerId, Is.Null);
        }

        // ---------- helper methods ----------
        private async Task SeedCursor(int current)
        {
            _db.Context.Cursors.RemoveRange(_db.Context.Cursors);
            _db.Context.Cursors.Add(new Cursor { Id = 1, Current = current });
            await _db.Context.SaveChangesAsync();
        }

        private async Task<int> SeedUnassignedTask(int orderId)
        {
            var task = new FulfillmentTask
            {
                OrderId = orderId,
                WorkerId = null,
                Status = "Pending"
            };
            _db.Context.FulfilmentTasks.Add(task);
            await _db.Context.SaveChangesAsync();
            return task.Id;
        }
        private async Task SeedActiveTasksForWorker(int workerId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _db.Context.FulfilmentTasks.Add(new FulfillmentTask
                {
                    OrderId = 10000 + workerId * 100 + i,
                    WorkerId = workerId,
                    Status = "Assigned"
                });
                _db.Context.Workers.Find(workerId)!.ActiveTasksCount++;
            }
            await _db.Context.SaveChangesAsync();
        }
    }
}
