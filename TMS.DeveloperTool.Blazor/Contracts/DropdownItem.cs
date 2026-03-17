namespace TMS.DeveloperTool.Blazor.Contracts;

public record DropdownItem<TKey>(TKey Id, string Code, string Name);

public sealed record DropdownItemPlanning(Guid Id, string Code, string Name, string Status)
    : DropdownItem<Guid>(Id, Code, Name);
