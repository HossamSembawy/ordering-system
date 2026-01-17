namespace FulfilmentService.Interfaces
{
    public interface IWorkerRepository
    {
        public Task<int> GetCountActiveTasks(int workerId);
    }
}
