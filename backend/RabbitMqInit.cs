using RabbitMQ.Client;

public sealed class RabbitMqInit
{
    //declaração das propriedades
    public IConnection _connection;
    public IChannel _channel;
    public SemaphoreSlim _publishLock;

    // método construtor 
    private RabbitMqInit (
        IConnection connection,
        IChannel channel,
        SemaphoreSlim publishLock
    )
    {
        _connection = connection;
        _channel = channel;
        _publishLock = publishLock;
    }

    // Inicialização dos serviços
    public static async Task<RabbitMqInit> CreateAsync(
    IConfiguration configuration)
    {
        var hostName = configuration["RabbitMq:HostName"]
            ?? throw new InvalidOperationException(
                "RabbitMq:HostName não foi configurado.");

        // Conexão com o Host;
        var connectionFactory = new ConnectionFactory
        {
            HostName = hostName
        };

        var connection = await connectionFactory.CreateConnectionAsync();

        var channel = await connection.CreateChannelAsync();

        var publishLock = new SemaphoreSlim(1, 1);

        return new RabbitMqInit(
            connection,
            channel,
            publishLock
        );
    }

    public async Task DeclareQueueAsync (
        string queueName)
    {
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

    }

}
