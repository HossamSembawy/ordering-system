using FluentAssertions;
using Fulfilment.Test.FulfilmentTestInfrastructure;
using FulfilmentService.Database.Seeding;
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Repositories;
using FulfilmentService.Services;
using Moq;
using System.Net;
using Xunit;

namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class TaskRepository
    {

        private SqliteInMemoryDb _db = default!;
        private ITaskService _repo = default!;
        private IWokerService _workerRepository = default!;

        [SetUp]
        public void SetUp()
        {

            _db = new SqliteInMemoryDb();


            if (!_db.Context.Cursors.Any())
            {
                _db.Context.Cursors.Add(new Cursor { Id = 1, Current = 0 });
                _db.Context.SaveChanges();
            }

            _workerRepository = new WorkerService(_db.Context);
            SeedWorkers.Seed(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }


        [Fact]
        public async Task CreateTask_ShouldCreatePendingTask_AndCallAddAsync()
        {
            // Arrange
            var uow = new Mock<IUnitOfWork>();

            var taskRepo = new Mock<IGenericRepository<FulfillmentTask>>();

            uow.SetupGet(x => x.taskRepository).Returns(taskRepo.Object);

            FulfillmentTask captured = null!;
            taskRepo.Setup(r => r.AddAsync(It.IsAny<FulfillmentTask>()))
                .Callback<FulfillmentTask>(t => captured = t)
                .ReturnsAsync((FulfillmentTask t) => t);

            var service = new TaskService(uow.Object);

            // Act
            var result = await service.CreateTask(orderId: 10);

            // Assert
            result.Should().NotBeNull();
            result.OrderId.Should().Be(10);
            result.Status.Should().Be("Pending");

            captured.Should().NotBeNull();
            captured.OrderId.Should().Be(10);
            captured.Status.Should().Be("Pending");

            taskRepo.Verify(r => r.AddAsync(It.IsAny<FulfillmentTask>()), Times.Once);
        }

    }
}
