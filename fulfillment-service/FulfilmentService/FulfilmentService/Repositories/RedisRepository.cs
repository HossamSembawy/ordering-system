using FulfilmentService.Interfaces;
using StackExchange.Redis;

namespace FulfilmentService.Repositories
{
    public class RedisRepository<T> : IRedisRepository<T> where T : class
    {
        private readonly IDatabase _redis;
        public RedisRepository(IConnectionMultiplexer connection)
        {
            _redis = connection.GetDatabase();
        }
        public async Task<bool> Delete(string id)
        {
            var result = await _redis.KeyDeleteAsync(id);
            return result;
        }

        public async Task<T> Get(string id)
        {
            var result = await _redis.StringGetAsync(id);
            return result.HasValue ? System.Text.Json.JsonSerializer.Deserialize<T>(result) : null;
        }

        public async Task<List<T>> GetList()
        {
            var keys = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First()).Keys();
            var list = new List<T>();
            foreach (var key in keys)
            {
                var item = _redis.StringGet(key);
                if (item.HasValue)
                {
                    var deserializedItem = System.Text.Json.JsonSerializer.Deserialize<T>(item);
                    if (deserializedItem != null)
                    {
                        list.Add(deserializedItem);
                    }
                }
            }
            return list;
        }

        public async Task<bool> Set(string id, T item)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(item);
            return await _redis.StringSetAsync(id, json);
        }
    }
}
