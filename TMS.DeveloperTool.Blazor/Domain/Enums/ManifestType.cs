using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum DeliveryManifestType : short
{
    [Description("Chuyển phát nhanh")]
    Express = 1,

    [Description("COD")]
    Cod = 2,
}
