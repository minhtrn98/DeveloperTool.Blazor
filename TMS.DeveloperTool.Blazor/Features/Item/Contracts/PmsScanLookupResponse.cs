using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Features.Item.Contracts;

public class PmsScanLookupResponse
{
    public string ScannedCode { get; set; } = string.Empty;
    public PmsScannedCodeType ScannedCodeType { get; set; }
    public string? HandoverCode { get; set; }
    public string? OriginPostOfficeCode { get; set; }
    public string? OriginPostOfficeName { get; set; }
    public string? DestinationPostOfficeCode { get; set; }
    public string? DestinationPostOfficeName { get; set; }
    public DateTime? IssuedAt { get; set; }
    public List<PmsScanItem> Items { get; set; } = new();
}

public class PmsScanItem
{
    public string ItemId { get; set; } = string.Empty;
    public short ItemLevel { get; set; }
    public string? OrderId { get; set; }
    public string? PackageCode { get; set; }
    public string? TripCode { get; set; }
    public string? DestinationPostOfficeCode { get; set; }
    public string? DestinationPostOfficeName { get; set; }
    public decimal? Weight { get; set; }
    public decimal? CalWeight { get; set; }
    public OrderType? OrderType { get; set; }
    public WarehouseStatus? WarehouseStatus { get; set; }
    public string? ReceiverAddress { get; set; }
    public string? ReceiverWardId { get; set; }
    public string? ReceiverWardName { get; set; }
    public string? ReceiverProvinceId { get; set; }
    public string? ReceiverProvinceName { get; set; }
    public string? ReceiverPostOfficeId { get; set; }
    public string? ReceiverPostOfficeName { get; set; }
    public string? ReceiverCountryId { get; set; }
    public string? ReceiverCountryName { get; set; }
    public decimal? CodAmount { get; set; }
}
