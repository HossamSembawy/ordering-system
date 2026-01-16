using FulfilmentService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FulfilmentService.Controllers
{
    [Route("api/[controller]")]
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
            if (result.Status is Models.Status.Created)
            {
                return Ok(result);
            }
            return Created();
        }
    }
}
