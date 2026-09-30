using System.Text.Json;
using RabbitMQ.Client;

// Puxa todas as configs para a aplicação rodar
var builder = WebApplication.CreateBuilder(args);

// Configura a dependencia - import openApi
builder.Services.AddOpenApi();

//Iniciando os serviços e elementos do MQ
var rabbitMq = await RabbitMqInit.CreateAsync(builder.Configuration);

var queueName = "order_created";
await rabbitMq.DeclareQueueAsync(queueName);

// Monta a aplicação
var app = builder.Build();

// Executa a dependencia de doc
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Endpoints
app.MapGet("/", () =>
{
    var forecast =  "banana";
    return forecast;
})
.WithName("Home");

app.MapPost("/orders", async (CreateOrderRequest request, CancellationToken cancellationToken) =>
{
    var orderCreated = new OrderCreated(
        Guid.NewGuid(),
        request.CustomerName,
        request.Pizza,
        DateTimeOffset.UtcNow);

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
    string Pizza
);

record OrderCreated (
    Guid OrderId,
    string CustomerName,
    string Pizza,
    DateTimeOffset CreatedAt
);
