namespace FulfilmentService.Interfaces
{
    public interface IWokerService
    {
        public Task<int> GetCountActiveTasks(int workerId);
    }
}
