using System.ComponentModel;

namespace TMS.DeveloperTool.Blazor.Domain.Enums;

public enum EvidenceType
{
    None = 0,

    [Description("Hình ảnh/Video")]
    ImageVideo = 1,

    [Description("Ký nhận điện tử")]
    ElectronicSignature = 2,

    [Description("QR code")]
    QRCode = 4,
}
