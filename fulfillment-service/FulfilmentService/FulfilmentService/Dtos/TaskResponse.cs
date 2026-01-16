using FulfilmentService.Models;

namespace FulfilmentService.Dtos
{
    public class TaskResponse
    {
        public int OrderId { get; set; }
        public Status Status{ get; set; }
    }
}
