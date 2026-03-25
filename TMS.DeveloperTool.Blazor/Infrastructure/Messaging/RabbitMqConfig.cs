namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

public sealed class RabbitMqConfig
{
    public const string SectionName = "MyRabbitMq";

    public required string HostName { get; init; }
    public required int Port { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public List<ExchangeConfig> Exchanges { get; init; } = [];

    public ExchangeConfig GetVehicleEventsExchange() => Exchanges[0];
    public ExchangeConfig GetPmsEventsExchange() => Exchanges[1];
}

public sealed class ExchangeConfig
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public Dictionary<string, QueueConfig> Queues { get; init; } = [];

    public QueueConfig GetVehicleQueue() => Queues["vehicles"];
    public QueueConfig GetPickupTasksQueue() => Queues["pickuptasks"];
}

public sealed class QueueConfig
{
    public required string QueueName { get; init; }
    public required string RoutingKey { get; init; }
    public required int PrefetchCount { get; init; }
}
