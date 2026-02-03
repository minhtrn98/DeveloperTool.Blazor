namespace TMS.DeveloperTool.Blazor.Entities;

public sealed class RouteCheckPointTemplate
{
    public Guid Id { get; set; }
    public int JumpSeconds { get; set; }

    public ICollection<RouteCheckPoint> RouteCheckPoints { get; set; } = [];
}
