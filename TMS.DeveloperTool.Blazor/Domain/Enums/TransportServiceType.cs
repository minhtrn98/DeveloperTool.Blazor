using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum TransportServiceType
{
    [Description("Chuyển phát nhanh")]
    DE,
    [Description("Chuyển phát nhanh tiết kiệm")]
    DS,
    [Description("Dịch vụ COD nhanh")]
    ED,
    [Description("Dịch vụ COD tiết kiệm")]
    EF,
    [Description("Ghi sổ quốc tế")]
    GSQ,
    [Description("Chuyển phát nhanh quốc tế")]
    IE,
    [Description("Chuyển phát tiết kiệm quốc tế")]
    IM,
    [Description("Dịch vụ hàng gom")]
    TC,
    [Description("Chuyển phát đường bộ")]
    TF,
    [Description("Chuyển phát đường bộ tiết kiệm")]
    TS
}