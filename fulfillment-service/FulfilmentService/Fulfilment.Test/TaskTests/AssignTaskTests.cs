
using Fulfilment.Test.FulfilmentTestInfrastructure;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Repositories;
using FulfilmentService.Services;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class AssignTaskTests
    {
        private SqliteInMemoryDb _db = default!;
        private ITaskService _repo = default!;

        [SetUp]
        public void SetUp()
        {
            _db = new SqliteInMemoryDb();

            // Ensure cursor exists
            if (!_db.Context.Cursors.Any())
            {
                _db.Context.Cursors.Add(new Cursor { Id = 1, Current = 0 });
                _db.Context.SaveChanges();
            }

            SeedWorkers.Seed(_db);

            var unitOfWork = new UnitOfWork(_db.Context);
            _repo = new TaskService(unitOfWork);
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        [Test]
        public async Task AssignTask_WhenTaskNotFound_ThrowsFailedToAssign()
        {
            var ex = Assert.ThrowsAsync<Exception>(async () => await _repo.AssignTask(999));
            Assert.That(ex!.Message, Is.EqualTo("Failed to assign a task"));
        }

        [Test]
        public async Task AssignTask_WhenTaskAlreadyAssigned_ThrowsFailedToAssign()
        {
            var task = new FulfillmentTask { OrderId = 1, WorkerId = 2, Status = "ASSIGNED" };
            _db.Context.FulfilmentTasks.Add(task);
            await _db.Context.SaveChangesAsync();

            var ex = Assert.ThrowsAsync<Exception>(async () => await _repo.AssignTask(task.Id));
            Assert.That(ex!.Message, Is.EqualTo("Failed to assign a task"));
        }
    }
}
