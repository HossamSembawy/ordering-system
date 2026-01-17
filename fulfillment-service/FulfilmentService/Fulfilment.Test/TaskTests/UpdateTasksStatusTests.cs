
using Fulfilment.Test.FulfilmentTestInfrastructure;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Repositories;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class UpdateTaskStatusTests
    {
        private SqliteInMemoryDb _db = default!;
        private IFulfilmentTaskRepository _repo = default!;
        private IWorkerRepository _workerRepository = default!;

        [SetUp]
        public void SetUp()
        {
            _db = new SqliteInMemoryDb();

            _workerRepository = new WorkerRepository(_db.Context);
            _repo = new FulfilmentTaskRepository(_db.Context, _workerRepository);

            SeedWorkers.Seed(_db); 
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        [Test]
        public void UpdateTaskStatus_WhenTaskNotFound_Throws()
        {
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Completed" };

            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _repo.UpdateTaskStatus(taskId: 99999, model: dto));

            Assert.That(ex!.Message, Is.EqualTo("Task not found"));
        }

        [Test]
        public async Task UpdateTaskStatus_WhenWorkerNotAssigned_Throws()
        {
            // Arrange
            var taskId = await SeedTask(workerId: 2, status: "Assigned");
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Completed" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _repo.UpdateTaskStatus(taskId, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Worker not assigned to this task"));
        }

        [TestCase("Completed")]
        [TestCase("Failed")]
        public async Task UpdateTaskStatus_WhenAlreadyProcessed_Throws(string existingStatus)
        {
            // Arrange
            var taskId = await SeedTask(workerId: 1, status: existingStatus);
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Completed" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _repo.UpdateTaskStatus(taskId, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Task already processed"));
        }

        [Test]
        public async Task UpdateTaskStatus_WhenAssigned_ToInvalidStatus_ThrowsInvalidTransition()
        {
            // Arrange: current status Assigned
            var taskId = await SeedTask(workerId: 1, status: "Assigned");

            // Try invalid transition e.g. Assigned -> Pending
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Pending" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _repo.UpdateTaskStatus(taskId, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Invalid Transition"));
        }

        [Test]
        public async Task UpdateTaskStatus_WhenAssigned_ToCompleted_UpdatesAndPersists()
        {
            // Arrange
            var taskId = await SeedTask(workerId: 1, status: "Assigned");
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Completed" };

            // Act
            var response = await _repo.UpdateTaskStatus(taskId, dto);

            // Assert response
            Assert.That(response.Id, Is.EqualTo(taskId));
            Assert.That(response.Status, Is.EqualTo("Completed"));

            // Assert persisted
            var persisted = await _db.Context.FulfilmentTasks.FindAsync(taskId);
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted!.Status, Is.EqualTo("Completed"));
        }

        [Test]
        public async Task UpdateTaskStatus_WhenAssigned_ToFailed_UpdatesAndPersists()
        {
            // Arrange
            var taskId = await SeedTask(workerId: 1, status: "Assigned");
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Failed" };

            // Act
            var response = await _repo.UpdateTaskStatus(taskId, dto);

            // Assert response
            Assert.That(response.Id, Is.EqualTo(taskId));
            Assert.That(response.Status, Is.EqualTo("Failed"));

            // Assert persisted
            var persisted = await _db.Context.FulfilmentTasks.FindAsync(taskId);
            Assert.That(persisted, Is.Not.Null);
            Assert.That(persisted!.Status, Is.EqualTo("Failed"));
        }

        // ---------- helpers ----------
        private async Task<int> SeedTask(int workerId, string status)
        {
            var task = new FulfilmentTask
            {
                OrderId = 1000 + Random.Shared.Next(1, 999),
                WorkerId = workerId,
                Status = status
            };

            _db.Context.FulfilmentTasks.Add(task);
            await _db.Context.SaveChangesAsync();
            return task.Id;
        }
    }
}
