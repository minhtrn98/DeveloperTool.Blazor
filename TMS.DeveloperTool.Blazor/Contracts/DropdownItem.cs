namespace TMS.DeveloperTool.Blazor.Contracts;

public sealed record DropdownItem<TKey>(TKey Id, string Code, string Name);
