namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public record DropdownItem<TKey>(TKey Id, string Code, string Name) : Interfaces.IDisplaySearchItem
{
    public virtual string GetDisplayString() => Name;

    public virtual bool Like(string searchTerm)
    {
        return Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record DropdownItemPlanning(Guid Id, string Code, string Name, string Status, string DepartmentCode)
    : DropdownItem<Guid>(Id, Code, Name)
{
    public override string GetDisplayString() => $"[{DepartmentCode}] [{Status}] {Name}";

    public override bool Like(string searchTerm)
    {
        return base.Like(searchTerm) ||
               DepartmentCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
               Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record VehicleDropdownItem(Guid Id, string Code, string LicensePlate, string DeptManagerCode)
    : DropdownItem<Guid>(Id, Code, LicensePlate)
{
    public override string GetDisplayString() => $"[{DeptManagerCode}] {LicensePlate}";

    public override bool Like(string searchTerm)
    {
        string formatValue = searchTerm.Replace(".", "").Replace("-", "");
        string formatLicensePlate = LicensePlate.Replace(".", "").Replace("-", "");
        return base.Like(searchTerm) ||
               formatLicensePlate.Contains(formatValue, StringComparison.OrdinalIgnoreCase) ||
               DeptManagerCode.Contains(formatValue, StringComparison.OrdinalIgnoreCase);
    }
}