using RabbitMQ.Client;
using System.Text;
using TMS.DeveloperTool.Blazor.Infrastructure.Constants;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

public sealed class EventService(RabbitMqConfig config, ILogger<EventService> logger, IWebHostEnvironment webHostEnvironment)
{
    private IConnection? _connection;

    public Task PublishPickupTaskEvent(string message, CancellationToken cancellationToken = default)
    {
        ExchangeConfig pmsEventsExchange = config.GetPmsEventsExchange();
        return PublishAsync(pmsEventsExchange, pmsEventsExchange.GetPickupTasksQueue(), message, cancellationToken);
    }

    public Task PublishOrderEvent(string message, CancellationToken cancellationToken = default)
    {
        ExchangeConfig pmsEventsExchange = config.GetPmsEventsExchange();
        return PublishAsync(pmsEventsExchange, pmsEventsExchange.GetOrdersQueue(), message, cancellationToken);
    }

    private async Task PublishAsync(ExchangeConfig exchange, QueueConfig queue, string message, CancellationToken cancellationToken)
    {
        if (!webHostEnvironment.IsEnvironment(ApplicationConstants.EnvironmentNames.Local))
        {
            logger.LogWarning("Blocked queue publish outside Local environment (current: {Environment}).", webHostEnvironment.EnvironmentName);
            throw new InvalidOperationException("Chỉ được bắn queue trên môi trường Local.");
        }

        if (_connection == null || !_connection.IsOpen)
        {
            await CreateConnectionAsync();
        }

        if (_connection == null)
        {
            logger.LogError("Failed to create a RabbitMQ connection. Message not sent.");
            return;
        }

        string queueName = queue.QueueName;
        using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        byte[] body = Encoding.UTF8.GetBytes(message);

        BasicProperties properties = new()
        {
            Persistent = true,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ContentType = "application/json",
            Headers = new Dictionary<string, object?>()
            {
            }
        };

        await channel.BasicPublishAsync(
            exchange: exchange.Name,
            routingKey: queue.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        logger.LogInformation("Message published to queue '{QueueName}': {Message}", queueName, message);
    }

    private async Task CreateConnectionAsync()
    {
        try
        {
            ConnectionFactory factory = new()
            {
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password
            };

            _connection = await factory.CreateConnectionAsync();
            logger.LogInformation("RabbitMQ connection created successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create a RabbitMQ connection.");
        }
    }
}
