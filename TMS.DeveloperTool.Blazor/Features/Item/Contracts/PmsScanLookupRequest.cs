using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Item.Contracts;

public class PmsScanLookupRequest
{
    public string Id { get; set; } = string.Empty;
    public PmsScannedCodeType Type { get; set; }
}
