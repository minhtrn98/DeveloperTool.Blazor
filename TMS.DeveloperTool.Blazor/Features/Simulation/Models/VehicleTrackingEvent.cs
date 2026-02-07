namespace TMS.DeveloperTool.Blazor.Features.Simulation.Models;

public sealed class VehicleTrackingEvent : IEvent
{
    public string EventType => "VehicleEvent";
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventId => Guid.CreateVersion7().ToString();
    DateTime IEvent.CreatedAt => DateTime.UtcNow;

    // Event-specific properties
    public VehicleTrackingData Data { get; set; } = new VehicleTrackingData();
}
