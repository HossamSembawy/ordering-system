using System.ComponentModel.DataAnnotations.Schema;

namespace FulfilmentService.Models
{
    public class FulfilmentTask
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int? WorkerId { get; set; }
        public Worker? Worker { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string Status { get; set; }
    }
}
