using FulfilmentService.Dtos;
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

        public TasksController(IFulfilmentTaskRepository fulfilmentTaskRepository)
        {
            _fulfilmentTaskRepository = fulfilmentTaskRepository;
        }
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] int OrderId)
        {
            var result = await _fulfilmentTaskRepository.CreateTask(OrderId);
            if (result.Status == "AlreadyExists")
            {
                return Ok(result);
            }
            return Created();
        }
        [HttpPost("Assign/{taskId}")]
        public async Task<IActionResult> AssignTask(int taskId)
        {
            try
            {
                var result = await _fulfilmentTaskRepository.AssignTask(taskId);
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
