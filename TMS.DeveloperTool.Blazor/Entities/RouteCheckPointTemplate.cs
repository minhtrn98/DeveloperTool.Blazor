namespace TMS.DeveloperTool.Blazor.Entities;

public sealed class RouteCheckPointTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public int JumpSeconds { get; set; } = 10;

    public ICollection<RouteCheckPoint> RouteCheckPoints { get; set; } = [];
}
