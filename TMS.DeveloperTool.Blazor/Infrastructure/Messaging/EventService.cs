using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using TMS.DeveloperTool.Blazor.Features.Simulation.Models;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

public sealed class EventService(RabbitMqConfig config, ILogger<EventService> logger)
{
    private IConnection? _connection;

    public async Task PublishTrackingEvent(VehicleTrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            await CreateConnectionAsync();
        }

        if (_connection == null)
        {
            logger.LogError("Failed to create a RabbitMQ connection. Message not sent.");
            return;
        }

        ExchangeConfig vehiclesExchange = config.GetVehicleEventsExchange();
        QueueConfig vehicleQueue = vehiclesExchange.GetVehicleQueue();

        string queueName = vehicleQueue.QueueName;
        using IChannel channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken);

        string message = JsonConvert.SerializeObject(trackingEvent, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None
        });

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
            exchange: vehiclesExchange.Name,
            routingKey: vehicleQueue.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        logger.LogInformation("Message published to queue '{QueueName}': {Message}", queueName, message);
    }

    public async Task PublishPickupTaskEvent(string message, CancellationToken cancellationToken = default)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            await CreateConnectionAsync();
        }

        if (_connection == null)
        {
            logger.LogError("Failed to create a RabbitMQ connection. Message not sent.");
            return;
        }

        ExchangeConfig pmsEventsExchange = config.GetPmsEventsExchange();
        QueueConfig pickupTaskQueue = pmsEventsExchange.GetPickupTasksQueue();

        string queueName = pickupTaskQueue.QueueName;
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
            exchange: pmsEventsExchange.Name,
            routingKey: pickupTaskQueue.RoutingKey,
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
