
using FulfilmentService.Dtos;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using FulfilmentService.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Fulfilment.Test.TaskTests
{
    [TestFixture]
    public class UpdateTaskStatusTests
    {
        private Mock<IUnitOfWork> _uow = default!;
        private Mock<IGenericRepository<FulfillmentTask>> _taskRepo = default!;
        private Mock<IGenericRepository<Worker>> _workerRepo = default!;
        private Mock<IDbContextTransaction> _tx = default!;
        private TaskService _service = default!;

        [SetUp]
        public void SetUp()
        {
            _uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
            _taskRepo = new Mock<IGenericRepository<FulfillmentTask>>(MockBehavior.Strict);
            _workerRepo = new Mock<IGenericRepository<Worker>>(MockBehavior.Strict);
            _tx = new Mock<IDbContextTransaction>(MockBehavior.Strict);

            _tx.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
            _tx.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
            _tx.Setup(t => t.Dispose()).Verifiable();

            _uow.SetupGet(x => x.taskRepository).Returns(_taskRepo.Object);
            _uow.SetupGet(x => x.WorkerRepository).Returns(_workerRepo.Object);

            _uow.Setup(x => x.BeginTransaction(false)).ReturnsAsync(_tx.Object);
            _uow.Setup(x => x.SaveChanges()).Returns(Task.CompletedTask);

            _service = new TaskService(_uow.Object);
        }

        [Test]
        public async Task UpdateTaskStatus_WhenTaskNotFound_ThrowsFailedAndRollsBack()
        {
            _taskRepo.Setup(r => r.GetByCondition(It.IsAny<Expression<Func<FulfillmentTask, bool>>>()))
                     .ReturnsAsync((FulfillmentTask?)null);

            var dto = new UpdateTaskDto { WorkerId = 1, Status = "COMPLETED" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateTaskStatus(99999, dto));

            Assert.That(ex!.Message, Is.EqualTo("Failed to update task status"));
            _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
            _tx.Verify(t => t.Dispose(), Times.Once);
        }

        [Test]
        public async Task UpdateTaskStatus_WhenWorkerMismatch_ThrowsFailedAndRollsBack()
        {
            // Arrange
            var task = new FulfillmentTask { Id = 10, WorkerId = 2, Status = "ASSIGNED" };

            _taskRepo.Setup(r => r.GetByCondition(It.IsAny<Expression<Func<FulfillmentTask, bool>>>()))
                     .ReturnsAsync(task);

            var dto = new UpdateTaskDto { WorkerId = 1, Status = "COMPLETED" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateTaskStatus(10, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Failed to update task status"));
            _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase("COMPLETED")]
        [TestCase("FAILED")]
        public async Task UpdateTaskStatus_WhenAlreadyProcessed_ThrowsFailedAndRollsBack(string existingStatus)
        {
            // Arrange
            var task = new FulfillmentTask { Id = 11, WorkerId = 1, Status = existingStatus };

            _taskRepo.Setup(r => r.GetByCondition(It.IsAny<Expression<Func<FulfillmentTask, bool>>>()))
                     .ReturnsAsync(task);

            var dto = new UpdateTaskDto { WorkerId = 1, Status = "COMPLETED" };

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateTaskStatus(11, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Failed to update task status"));
            _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task UpdateTaskStatus_WhenAssignedToInvalidStatus_ThrowsFailedAndRollsBack()
        {
            // Arrange
            var task = new FulfillmentTask { Id = 12, WorkerId = 1, Status = "ASSIGNED" };

            _taskRepo.Setup(r => r.GetByCondition(It.IsAny<Expression<Func<FulfillmentTask, bool>>>()))
                     .ReturnsAsync(task);

            var dto = new UpdateTaskDto { WorkerId = 1, Status = "Pending" }; // invalid

            // Act
            var ex = Assert.ThrowsAsync<Exception>(async () =>
                await _service.UpdateTaskStatus(12, dto));

            // Assert
            Assert.That(ex!.Message, Is.EqualTo("Failed to update task status"));
            _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase("COMPLETED")]
        [TestCase("FAILED")]
        public async Task UpdateTaskStatus_WhenAssignedToFinalStatus_UpdatesWorker_SaveAndCommit(string newStatus)
        {
            // Arrange
            var task = new FulfillmentTask { Id = 13, WorkerId = 1, Status = "ASSIGNED" };
            var worker = new Worker { Id = 1, ActiveTasksCount = 3 };

            _taskRepo.Setup(r => r.GetByCondition(It.IsAny<Expression<Func<FulfillmentTask, bool>>>()))
                     .ReturnsAsync(task);

            _workerRepo.Setup(r => r.GetWithLocking(1, "Workers"))
                      .ReturnsAsync(worker);

            var dto = new UpdateTaskDto { WorkerId = 1, Status = newStatus };

            // Act
            var result = await _service.UpdateTaskStatus(13, dto);

            // Assert
            Assert.That(result.Status, Is.EqualTo(newStatus));
            Assert.That(worker.ActiveTasksCount, Is.EqualTo(2)); // 3 -> 2

            _uow.Verify(x => x.SaveChanges(), Times.Once);
            _tx.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            _tx.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
