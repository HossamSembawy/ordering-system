using FulfilmentService.BackgroundJob;
using FulfilmentService.Database;
using FulfilmentService.ExternalClients;
using FulfilmentService.Interfaces;
using FulfilmentService.Repositories;
using FulfilmentService.Services;
using Microsoft.EntityFrameworkCore;

namespace FulfilmentService
{
    public static class DependencyInjection
    {
        public static void AddFulfilmentServiceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<QueuedProcessorBackgroundService>();
            services.AddHostedService<BackgroundRefresh>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IWokerService, WorkerService>();
            services.AddScoped<IOrderServiceClient, OrderServiceClient>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddDbContext<FulfilmentDbContext>(opts =>
            {
                opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(client =>
            {
                var baseUrl = configuration["FulfillmentService:BaseUrl"] ?? "https://localhost:7017";
                client.BaseAddress = new Uri(baseUrl);
            });
        }
    }
}
