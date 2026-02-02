using FulfilmentService.BackgroundJob;
using FulfilmentService.Database;
using FulfilmentService.ExternalClients;
using FulfilmentService.Interfaces;
using FulfilmentService.Repositories;
using FulfilmentService.Services;
using FulfilmentService.Strategies;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace FulfilmentService
{
    public static class DependencyInjection
    {
        public static void AddFulfilmentServiceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {

                var conf = configuration["Redis:ConnectionString"];
                return ConnectionMultiplexer.Connect(conf!);
            });
            services.AddScoped<IAssignmentStrategy, RoundRobinStrategy>();
            services.AddScoped(typeof(IRedisRepository<>), typeof(RedisRepository<>));
            services.AddLogging(opt => { opt.AddConsole(); });
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
