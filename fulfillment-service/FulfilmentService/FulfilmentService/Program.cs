
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

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddScoped<IFulfilmentTaskRepository, FulfilmentTaskRepository>();
            builder.Services.AddScoped<IWorkerRepository, WorkerRepository>();
            builder.Services.AddScoped<IOrderServiceClient, OrderServiceClient>();
            builder.Services.AddDbContext<FulfilmentDbContext>(opts =>
            {
                opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
            {
                var baseUrl = builder.Configuration["FulfillmentService:BaseUrl"] ?? "http://localhost:5001";
                client.BaseAddress = new Uri(baseUrl);
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                //using (var scope = app.Services.CreateScope())
                //{
                //    var dbContext = scope.ServiceProvider.GetRequiredService<FulfilmentDbContext>();
                //    WorkerSeeder.SeedWorkers(dbContext);
                //}
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
