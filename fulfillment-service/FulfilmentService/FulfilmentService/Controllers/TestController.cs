using FulfilmentService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace FulfilmentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IRedisRepository<string> _redisRepository;

        public TestController(IRedisRepository<string> redisRepository)
        {
            _redisRepository = redisRepository;
        }

        [HttpGet("cache/{key}")]
        public async Task<IActionResult> GetCacheValue(string key)
        {
            var value = await _redisRepository.Get(key);
            if (value == null)
            {
                return NotFound();
            }
            return Ok(value);
        }
        [HttpPost("cache/{key}")]
        public async Task<IActionResult> SetCacheValue(string key, [FromBody] string value)
        {
            await _redisRepository.Set(key, value);
            return Ok();
        }
    }
}
