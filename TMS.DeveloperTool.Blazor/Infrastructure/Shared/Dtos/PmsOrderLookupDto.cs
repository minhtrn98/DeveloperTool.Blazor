using TMS.DeveloperTool.Blazor.Domain.Enums;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Shared.Dtos;

public sealed record PmsOrderLookupDto
{
    public string OrderId { get; init; } = string.Empty;
    public DateTimeOffset? AcceptedTime { get; init; }
    public string? CurrentPostOfficeCode { get; init; }
    public string? CurrentPostOfficeName { get; init; }
    public string? ReceiverPostOfficeCode { get; init; }
    public string? ReceiverPostOfficeName { get; init; }
    public string? ReceiverAddress { get; init; }
    public string? ReceiverWardId { get; init; }
    public string? ReceiverWardName { get; init; }
    public string? ReceiverProvinceId { get; init; }
    public string? ReceiverProvinceName { get; init; }
    public string? ReceiverPostOfficeId { get; init; }
    public string? ReceiverCountryId { get; init; }
    public string? ReceiverCountryName { get; init; }
    public decimal? CodAmount { get; init; }
    public OrderType OrderType { get; init; }
}
