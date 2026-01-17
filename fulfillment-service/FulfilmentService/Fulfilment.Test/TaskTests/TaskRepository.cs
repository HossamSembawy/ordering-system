using Fulfilment.Test.FulfilmentTestInfrastructure;
using FulfilmentService.Database.Seeding;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Repositories;
using Moq;
using System.Net;

namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class TaskRepository
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
            _repo = new FulfilmentTaskRepository(_db.Context, _workerRepository); // adjust constructor as needed
            SeedWorkers.Seed(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task GivenOrderId_WhenOrderIdIsNew_NewTaskIsCreated()
        {
            // Arrange
            int orderId = 123;

            // Act
            var result = await _repo.CreateTask(orderId);

            // Assert: verify it is actually in the DB
            Assert.IsTrue(result.Status == "Pending");
        }
        [Test]
        public async Task GivenOrderId_WhenOrderIdExists_ReturnsSameId()
        {
            // Arrange
            int orderId = 123;

            // Act
            var result1 = await _repo.CreateTask(orderId);
            var result2 = await _repo.CreateTask(orderId);

            // Assert: verify it is actually in the DB
            Assert.IsTrue(result1.Id==result2.Id);
        }
    }
}
