namespace TMS.DeveloperTool.Blazor.Features.UnloadingHandover.Models;

/// <summary>Status filter values for the pending unloading handover page.</summary>
public static class UnloadingHandoverPendingStatus
{
    /// <summary>Chưa nhận hoặc chưa xác nhận (default when nothing is selected).</summary>
    public const string Any = "Any";
    public const string NotReceived = "NotReceived";
    public const string NotConfirmed = "NotConfirmed";

    public static readonly IReadOnlyList<EnumOption> Options =
    [
        new(Any, "Chưa nhận hoặc chưa xác nhận"),
        new(NotReceived, "Chưa nhận"),
        new(NotConfirmed, "Chưa xác nhận")
    ];
}
