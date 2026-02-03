namespace TMS.DeveloperTool.Blazor.Models;

public sealed class VehicleTrackingEvent : IEvent
{
    public string EventType => "VehicleEvent";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventId => Guid.NewGuid().ToString();
    DateTime IEvent.CreatedAt => DateTime.UtcNow;

    // Event-specific properties
    public VehicleTrackingData Data { get; set; } = new VehicleTrackingData();
}
