using FulfilmentService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FulfilmentService.Database.Configurations
{
    public class FulfilmentTaskConfigurations : IEntityTypeConfiguration<FulfilmentTask>
    {
        public void Configure(EntityTypeBuilder<FulfilmentTask> builder)
        {
            builder.HasIndex(ft => ft.OrderId).IsUnique();
        }
    }
}
