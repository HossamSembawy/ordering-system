
using FulfilmentService.BackgroundJob;
using FulfilmentService.Controllers;
using FulfilmentService.Dtos;
using FulfilmentService.ExternalClients;
using FulfilmentService.Interfaces;
using FulfilmentService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fulfilment.Test.ControllerTests
{
    [TestFixture]
    public class TaskControllerTests
    {
        private Mock<ITaskService> _taskServiceMock = null!;
        private Mock<IOrderServiceClient> _orderClientMock = null!;
        private Mock<IBackgroundTaskQueue> _queueMock = null!;
        private TasksController _controller = null!;

        [SetUp]
        public void Setup()
        {
            _taskServiceMock = new Mock<ITaskService>(MockBehavior.Strict);
            _orderClientMock = new Mock<IOrderServiceClient>(MockBehavior.Strict);
            _queueMock = new Mock<IBackgroundTaskQueue>(MockBehavior.Strict);

            _controller = new TasksController(
                _taskServiceMock.Object,
                _orderClientMock.Object,
                _queueMock.Object
            );
        }


        [Test]
        public async Task CreateTask_WhenCreateSucceeds_ReturnsCreatedAtAction()
        {
            // Arrange
            int orderId = 10;
            var createdTask = new FulfillmentTask { Id = 123, OrderId = orderId, Status = "Pending" };

            _taskServiceMock
                .Setup(s => s.CreateTask(orderId))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _controller.CreateTask(orderId);

            // Assert
            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());

            var created = (CreatedAtActionResult)result;
            Assert.That(created.ActionName, Is.EqualTo("GetTask"));
            Assert.That(created.RouteValues, Is.Not.Null);
            Assert.That(created.RouteValues!["taskId"], Is.EqualTo(createdTask.Id));
            Assert.That(created.Value, Is.EqualTo(createdTask));

            _taskServiceMock.Verify(s => s.CreateTask(orderId), Times.Once);
        }

        [Test]
        public async Task CreateTask_WhenCreateThrows_ReturnsOkWithExistingTask()
        {
            // Arrange
            int orderId = 10;

            _taskServiceMock
                .Setup(s => s.CreateTask(orderId))
                .ThrowsAsync(new Exception("duplicate"));

            var existingTask = new FulfillmentTask { Id = 555, OrderId = orderId, Status = "ASSIGNED" };

            _taskServiceMock
                .Setup(s => s.GetByOrderId(orderId))
                .ReturnsAsync(existingTask);

            // Act
            var result = await _controller.CreateTask(orderId);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.EqualTo(existingTask));

            _taskServiceMock.Verify(s => s.CreateTask(orderId), Times.Once);
            _taskServiceMock.Verify(s => s.GetByOrderId(orderId), Times.Once);
        }


        [Test]
        public async Task UpdateTaskStatus_WhenSuccess_ReturnsOk_AndQueuesBackgroundWork()
        {
            // Arrange
            int taskId = 1;
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "COMPLETED" };

            var updatedTask = new FulfillmentTask
            {
                Id = taskId,
                WorkerId = 1,
                Status = "COMPLETED",
                OrderId = 100
            };

            _taskServiceMock
                .Setup(s => s.UpdateTaskStatus(taskId, dto))
                .ReturnsAsync(updatedTask);

            // Capture the background work item passed to the queue
            Func<IServiceProvider, CancellationToken, Task>? capturedWorkItem = null;

            _queueMock
                .Setup(q => q.QueueBackgroundWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()))
                .Callback<Func<IServiceProvider, CancellationToken, Task>>(workItem => capturedWorkItem = workItem);

            // Act
            var result = await _controller.UpdateTaskStatus(taskId, dto);

            // Assert: controller response
            Assert.That(result, Is.TypeOf<OkObjectResult>());

            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.EqualTo(updatedTask));

            // Assert: work item was queued
            Assert.That(capturedWorkItem, Is.Not.Null);

            _taskServiceMock.Verify(s => s.UpdateTaskStatus(taskId, dto), Times.Once);
            _queueMock.Verify(q => q.QueueBackgroundWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()), Times.Once);


            var services = new ServiceCollection()
                .AddLogging()
                .AddSingleton(_orderClientMock.Object)
                .BuildServiceProvider();

            await capturedWorkItem!(services, CancellationToken.None);

        }

        [Test]
        public async Task UpdateTaskStatus_WhenServiceThrows_ReturnsBadRequest_AndDoesNotQueue()
        {
            // Arrange
            int taskId = 1;
            var dto = new UpdateTaskDto { WorkerId = 1, Status = "FAILED" };

            _taskServiceMock
                .Setup(s => s.UpdateTaskStatus(taskId, dto))
                .ThrowsAsync(new Exception("Invalid Transition"));

            // IMPORTANT: queue should not be called
            _queueMock
                .Setup(q => q.QueueBackgroundWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()))
                .Throws(new Exception("Queue should not be called"));

            // Act
            var result = await _controller.UpdateTaskStatus(taskId, dto);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());

            var bad = (BadRequestObjectResult)result;
            Assert.That(bad.Value, Is.EqualTo("Invalid Transition"));

            _taskServiceMock.Verify(s => s.UpdateTaskStatus(taskId, dto), Times.Once);
            _queueMock.Verify(q => q.QueueBackgroundWorkItem(It.IsAny<Func<IServiceProvider, CancellationToken, Task>>()), Times.Never);
        }

        // ----------------------------
        // GetTask tests
        // ----------------------------

        [Test]
        public async Task GetTask_ReturnsOkWithTask()
        {
            // Arrange
            int taskId = 10;
            var task = new FulfillmentTask { Id = taskId, Status = "ASSIGNED", OrderId = 999, WorkerId = 3 };

            _taskServiceMock
                .Setup(s => s.Get(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            Assert.That(result, Is.TypeOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.EqualTo(task));

            _taskServiceMock.Verify(s => s.Get(taskId), Times.Once);
        }
    }
}
