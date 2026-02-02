namespace FulfilmentService.Interfaces
{
    public interface IRedisRepository<T> where T : class
    {
        Task<T> Get(string id);
        Task<List<T>> GetList();
        Task<bool> Set(string id, T item);
        Task<bool> Delete(string id);
    }
}
