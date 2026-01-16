using FulfilmentService.Models;

namespace FulfilmentService.Dtos
{
    public class TaskResponse
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Status{ get; set; }
    }
}
