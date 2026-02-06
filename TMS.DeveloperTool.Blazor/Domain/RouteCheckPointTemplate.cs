namespace TMS.DeveloperTool.Blazor.Domain;

public sealed class RouteCheckPointTemplate
{
    public RouteCheckPointTemplate()
    {
    }

    public static RouteCheckPointTemplate Copy(RouteCheckPointTemplate data)
    {
        return new RouteCheckPointTemplate
        {
            Id = data.Id,
            Name = data.Name,
            JumpSeconds = data.JumpSeconds,
            RouteCheckPoints = [.. data.RouteCheckPoints.Select(cp => RouteCheckPoint.Copy(cp))]
        };
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public int JumpSeconds { get; set; } = 10;

    public ICollection<RouteCheckPoint> RouteCheckPoints { get; set; } = [];
}
