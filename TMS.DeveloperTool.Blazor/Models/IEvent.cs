namespace TMS.DeveloperTool.Blazor.Models;

public interface IEvent
{
    string EventId { get; }

    DateTime CreatedAt { get; }

    string EventType { get; }
}
