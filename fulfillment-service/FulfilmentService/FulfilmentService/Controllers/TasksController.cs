using FulfilmentService.BackgroundJob;
using FulfilmentService.Dtos;
using FulfilmentService.ExternalClients;
using FulfilmentService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FulfilmentService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly IOrderServiceClient _orderClient;
        private readonly IBackgroundTaskQueue _taskQueue;


        public TasksController(ITaskService taskService, IOrderServiceClient orderClient, IBackgroundTaskQueue taskQueue)
        {
            _taskService = taskService;
            _orderClient = orderClient;
            _taskQueue = taskQueue;
        }
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] int OrderId)
        {
            try
            {
                var result = await _taskService.CreateTask(OrderId);
                return CreatedAtAction("GetTask", new { taskId = result.Id }, result);
            }
            catch (Exception ex)
            {
                return Ok(await _taskService.GetByOrderId(OrderId));
            }
        }
        
        [HttpPatch("{taskId}/status")]
        [ProducesErrorResponseType(typeof(BadRequestObjectResult))]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, UpdateTaskDto model)
        {
            try
            {
                var result = await _taskService.UpdateTaskStatus(taskId, model);
                _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<TasksController>>();
                    var orderServiceClient = serviceProvider.GetRequiredService<IOrderServiceClient>();

                    logger.LogInformation("Communication with Order Service started");

                    try
                    {
                        await orderServiceClient.UpdateOrderStatus(result);
                        logger.LogInformation("Communication with Order Service completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error occurred while Communicating with Order Service.");
                    }
                });
                // try to assign worker to any needed task
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("/{taskId}")]
        [ActionName("GetTask")]
        public async Task<IActionResult> GetTask(int taskId)
        {
            var result = await _taskService.Get(taskId);
            return Ok(result);
        }
    }
}
