using System.Text.Json.Serialization;

namespace TMS.DeveloperTool.Blazor.Features.Tracking.Dtos;

public sealed class VehicleStatusResponseDto
{
    [JsonPropertyName("Data")]
    public IReadOnlyList<VehicleStatusDto> Data { get; init; } = [];
}

public sealed class VehicleStatusDto
{
    [JsonPropertyName("Driver")]
    public string? Driver { get; init; }

    [JsonPropertyName("Plate")]
    public string? Plate { get; init; }

    [JsonPropertyName("ActualPlate")]
    public string? ActualPlate { get; init; }

    [JsonPropertyName("LicenseNo")]
    public string? LicenseNo { get; init; }
}
