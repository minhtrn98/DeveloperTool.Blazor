using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;
using TMS.DeveloperTool.Blazor.Features.Simulation.Models;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

public sealed class EventService
{
    private readonly ILogger<EventService> _logger;
    private readonly RabbitMqConfig _config;
    private IConnection? _connection;

    public EventService(IOptions<RabbitMqConfig> options, ILogger<EventService> logger)
    {
        _config = options.Value;
        _logger = logger;
    }

    public async Task PublishTeckingEvent(VehicleTrackingEvent trackingEvent, CancellationToken cancellationToken = default)
    {
        if (_connection == null || !_connection.IsOpen)
        {
            await CreateConnectionAsync();
        }

        if (_connection == null)
        {
            _logger.LogError("Failed to create a RabbitMQ connection. Message not sent.");
            return;
        }

        string queueName = _config.Exchanges[0].Queues["vehicles"].QueueName;
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
            exchange: _config.Exchanges[0].Name,
            routingKey: _config.Exchanges[0].Queues["vehicles"].RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Message published to queue '{QueueName}': {Message}", queueName, message);
    }

    private async Task CreateConnectionAsync()
    {
        try
        {
            ConnectionFactory factory = new()
            {
                HostName = _config.HostName,
                UserName = _config.UserName,
                Password = _config.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection created successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not create a RabbitMQ connection.");
        }
    }
}
