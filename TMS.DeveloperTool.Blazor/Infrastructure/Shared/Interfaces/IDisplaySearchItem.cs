namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Interfaces;

public interface IDisplaySearchItem
{
    string GetDisplayString();
    bool Like(string searchTerm);
}
