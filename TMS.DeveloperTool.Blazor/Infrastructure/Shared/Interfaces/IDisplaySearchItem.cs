namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

public interface IDisplaySearchItem
{
    string DisplayString { get; }
    bool Like(string searchTerm);
}
