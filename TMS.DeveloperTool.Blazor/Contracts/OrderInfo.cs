namespace TMS.DeveloperTool.Blazor.Contracts;

public sealed record OrderInfo(string OrderId, DateTime CreatedAt, bool HasPickupTask);