namespace TMS.DeveloperTool.Blazor.Infrastructure.Messaging;

/// <summary>
/// Singleton in-process pub/sub for broadcasting department selection changes
/// across all active Blazor circuits (tabs) of the same browser.
/// Key is <c>BrowserId</c> so only tabs of the same browser receive the event.
/// </summary>
public sealed class DepartmentChangedNotifier
{
    public event Action<string, DepartmentDto?>? OnDepartmentChanged;

    public void Notify(string browserId, DepartmentDto? department)
        => OnDepartmentChanged?.Invoke(browserId, department);
}
