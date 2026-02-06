namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

public interface IEvent
{
    string EventId { get; }

    DateTime CreatedAt { get; }

    string EventType { get; }
}
