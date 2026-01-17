using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OrderService.contacts;
using OrderService.Services;
using OrderService.Data;
using OrderService.Models;
using OrderService.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Register DbContext
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<IOrderService, OrdersService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();
builder.Services.AddScoped<OrderWorkflowService>();

builder.Services.AddHttpClient<IFulfillmentClient, HttpFulfillmentClient>(client =>
{
    var baseUrl = builder.Configuration["FulfillmentService:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    InventorySeeding.SeedInventory(dbContext);
}


app.MapPost("/orders", async (
    CreateOrderRequest request,
    OrderWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var order = await workflowService.PlaceOrderAsync(request.UserId, request.Items, cancellationToken);
        return Results.Created($"/orders/{order.OrderId}", new
        {
            orderId = order.OrderId,
            status = order.Status,
            createdAt = order.CreatedAt
        });
    }
    catch (OrderPlacementException ex) when (ex.Code == "INVALID_REQUEST")
    {
        return Results.BadRequest(new { error = ex.Code, message = ex.Message });
    }
    catch (OrderPlacementException ex) when (ex.Code == "PRODUCT_NOT_FOUND")
    {
        return Results.NotFound(new { error = ex.Code, message = ex.Message });
    }
    catch (OrderPlacementException ex) when (ex.Code == "INSUFFICIENT_STOCK")
    {
        return Results.Conflict(new { error = ex.Code, message = ex.Message });
    }
});

app.MapPost("/orders/{orderId:int}/fulfillment", async (
    int orderId,
    FulfillmentUpdateRequest request,
    OrderWorkflowService workflowService,
    CancellationToken cancellationToken) =>
{
    var updated = await workflowService.ApplyFulfillmentUpdateAsync(orderId, request.Status, request.WorkerId, cancellationToken);
    return updated ? Results.Ok() : Results.NotFound();
});

app.MapGet("/orders/{orderId:int}", async (int orderId, OrderDbContext context, CancellationToken cancellationToken) =>
{
    var order = await context.Orders
        .Include(o => o.Items)
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

    return order == null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public record CreateOrderRequest(int UserId, List<OrderItemRequest> Items);

public record FulfillmentUpdateRequest(string Status, string? WorkerId);
