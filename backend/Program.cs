using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PizzaFlow.Data;
using PizzaFlow.Models;
using RabbitMQ.Client;

// Puxa todas as configs para a aplicação rodar
var builder = WebApplication.CreateBuilder(args);

// Configura a dependencia - import openApi
builder.Services.AddOpenApi();
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalFrontend", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

//Iniciando os serviços e elementos do MQ
var rabbitMq = await RabbitMqInit.CreateAsync(builder.Configuration);

var queueName = "order_created";
await rabbitMq.DeclareQueueAsync(queueName);

// Monta a aplicação
var app = builder.Build();
await app.ApplyDatabaseMigrationsAsync();

// Executa a dependencia de doc
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("LocalFrontend");
}

//Endpoints
app.MapGet("/orders", async (PizzaFlowDbContext dbContext, CancellationToken cancellationToken) =>
{
    var orders = await dbContext.Orders
        .AsNoTracking()
        .Where(order => order.Status == OrderStatus.EmPreparo)
        .ToListAsync(cancellationToken);

    return Results.Ok(orders.Select(order => new
    {
        order.Id,
        order.CustomerName,
        order.Phone,
        order.Items,
        Status = order.Status.ToString(),
        order.CreatedAt
    }));
});

app.MapPost("/orders", async (
    CreateOrderRequest request,
    PizzaFlowDbContext dbContext,
    CancellationToken cancellationToken) =>
{

    Console.WriteLine("Criando novo pedido.");

    var order = new Order
    {
        CustomerName = request.CustomerName,
        Phone = request.Phone,
        Items = JsonSerializer.SerializeToDocument(
            request.Items,
            JsonSerializerOptions.Web)
    };

    dbContext.Orders.Add(order);
    await dbContext.SaveChangesAsync(cancellationToken);

    var orderCreated = new OrderCreated(
        order.Id,
        order.CustomerName,
        order.Phone,
        request.Items,
        order.Status,
        order.CreatedAt);

    var body = JsonSerializer.SerializeToUtf8Bytes(orderCreated);
    var properties = new BasicProperties
    {
        ContentType = "application/json",
        DeliveryMode = DeliveryModes.Persistent,
        Type = nameof(OrderCreated)
    };

    await rabbitMq._publishLock.WaitAsync(cancellationToken);

    try
    {
        await rabbitMq._channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
    finally
    {
        rabbitMq._publishLock.Release();
    }

    return Results.Accepted(value: orderCreated);
});

await app.RunAsync();

// "Payloads" - Como se fossem classes
record CreateOrderRequest (
    string CustomerName,
    string Phone,
    IReadOnlyList<OrderItem> Items
);

record OrderCreated (
    Guid OrderId,
    string CustomerName,
    string Phone,
    IReadOnlyList<OrderItem> Items,
    OrderStatus Status,
    DateTimeOffset CreatedAt
);
