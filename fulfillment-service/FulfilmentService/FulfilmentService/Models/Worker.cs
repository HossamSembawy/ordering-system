using System.ComponentModel.DataAnnotations;

namespace FulfilmentService.Models
{
    public class Worker
    {
        public int Id { get; set; } 
        public string Name { get; set; }
        public int ActiveTasksCount { get; set; }
    }
}
