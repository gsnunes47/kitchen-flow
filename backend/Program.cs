using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using KitchenFlow.Data;
using KitchenFlow.Models;
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

// Read
app.MapGet("/orders", async (KitchenFlow.Data.DbContext dbContext, CancellationToken cancellationToken) =>
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

// Create
app.MapPost("/orders", async (
    CreateOrderRequest request,
    KitchenFlow.Data.DbContext dbContext,
    CancellationToken cancellationToken) =>
{
    Console.WriteLine("Criando novo pedido.");

    //Persistencia
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

    // Mensageria
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

app.MapPut("/orders/{id}", async (
    Guid id,
    OrderPayload request,
    KitchenFlow.Data.DbContext dbContext,
    CancellationToken cancellationToken
) =>
{
    Console.WriteLine("Atualizando pedido");

    var order = await dbContext.Orders.FindAsync([id], cancellationToken);

    if (order is null)
    {
        return Results.NotFound();
    }

    // Pattern Matching
    if (request.status is OrderStatus status)
    {
        order.Status = status;
    }

    if (request.items != null)
    {
        order.Items = JsonSerializer.SerializeToDocument(
            request.items,
            JsonSerializerOptions.Web);
    }

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok(new
    {
        order.Id,
        order.CustomerName,
        order.Phone,
        order.Items,
        Status = order.Status.ToString(),
        order.CreatedAt,
        order.TotalPrice
    });
});

await app.RunAsync();

// Payloads
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

record OrderPayload (
    OrderStatus? status,
    IReadOnlyList<OrderItem>? items
);
