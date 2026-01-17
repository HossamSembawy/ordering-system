using FulfilmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfilmentService.Database.Configurations
{
    public class FulfilmentTaskConfigurations : IEntityTypeConfiguration<FulfillmentTask>
    {
        public void Configure(EntityTypeBuilder<FulfillmentTask> builder)
        {
            builder.HasIndex(ft => ft.OrderId).IsUnique();
        }
    }
}
