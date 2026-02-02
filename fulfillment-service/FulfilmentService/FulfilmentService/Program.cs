
using FulfilmentService.BackgroundJob;
using FulfilmentService.Database;
using FulfilmentService.Database.Seeding;
using FulfilmentService.ExternalClients;
using FulfilmentService.Interfaces;
using FulfilmentService.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FulfilmentService
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddFulfilmentServiceDependencies(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<FulfilmentDbContext>();
                    WorkerSeeder.SeedWorkers(dbContext);
                }
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
