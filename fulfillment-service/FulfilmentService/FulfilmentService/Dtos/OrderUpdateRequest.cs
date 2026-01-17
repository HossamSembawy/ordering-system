using System.Text.Json.Serialization;

namespace FulfilmentService.Dtos
{
    public class OrderUpdateRequest
    {
        [JsonIgnore]
        public int OrderId { get; set; }
        public int? WorkerId { get; set; }
        public string Status { get; set; }
    }
}
