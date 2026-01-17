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
        private readonly IFulfilmentTaskRepository _fulfilmentTaskRepository;
        private readonly IOrderServiceClient _orderClient;

        public TasksController(IFulfilmentTaskRepository fulfilmentTaskRepository, IOrderServiceClient orderClient)
        {
            _fulfilmentTaskRepository = fulfilmentTaskRepository;
            _orderClient = orderClient;
        }
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] int OrderId)
        {
            var result = await _fulfilmentTaskRepository.CreateTask(OrderId);
            if (result.Status == "AlreadyExists")
            {
                return Ok(result);
            }
            return CreatedAtAction("GetTask", new { taskId = result.Id }, result);
        }
        [HttpPost("{taskId}/Assign")]
        public async Task<IActionResult> AssignTask(int taskId)
        {
            try
            {
                var result = await _fulfilmentTaskRepository.AssignTask(taskId);
                var task = await _fulfilmentTaskRepository.Get(taskId);
                await _orderClient.UpdateOrderStatus(task);
                return Ok(result);
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch("/{taskId}/status")]
        [ProducesErrorResponseType(typeof(BadRequestObjectResult))]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, UpdateTaskDto model)
        {
            try
            {
                var result = await _fulfilmentTaskRepository.UpdateTaskStatus(taskId, model);
                var task = await _fulfilmentTaskRepository.Get(taskId);
                await _orderClient.UpdateOrderStatus(task);
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
            var result = await _fulfilmentTaskRepository.Get(taskId);
            return Ok(result);
        }
    }
}
