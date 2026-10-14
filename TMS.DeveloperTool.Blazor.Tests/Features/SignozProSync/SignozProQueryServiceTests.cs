using System.Text.Json;
using TMS.DeveloperTool.Blazor.Features.OrderStep1.Contracts;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Models;
using TMS.DeveloperTool.Blazor.Features.SignozProSync.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.SignozProSync;

public class SignozProQueryServiceTests
{
    // Trimmed from real SigNoz Pro rows: attributes_number is rounded to 3 decimals, the body is not.
    private const string OrderLocationRowJson = """
    {
      "data": {
        "attributes_bool": {},
        "attributes_number": { "ElapsedMs": 1, "Lat": 21.183, "Long": 106.075 },
        "attributes_string": { "FL": "DELIVERY", "Month": "2026-10-01", "OrderId": "90065787968", "Source": "PMS" },
        "body": "DELIVERY - [ORDER-LOCATION] Upserted 90065787968 month=10/1/2026 lat=21.18257600000004 long=106.07455085300006 source=PMS elapsedMs=1",
        "id": "0m7N5ZbXgOYhS9FTv3WzB20fjXh",
        "span_id": "c985c469a50e1221",
        "trace_id": "f009fb6c1d09b13161eb54b6e896f111"
      },
      "timestamp": "2026-10-14T10:19:43.5236366+07:00"
    }
    """;

    private const string ArrivalRowJson = """
    {
      "data": {
        "attributes_bool": { "IsGpsValid": false },
        "attributes_number": { "Distance": 376, "Lat": 21.231, "Lng": 105.592 },
        "attributes_string": {
          "ArrivalId": "3a244a99-4bac-e1cd-b8fe-67d5f68dcb42",
          "DE": "K1A-SP26104000",
          "OrderId": "WBJIL266D765215206",
          "Superseded": "",
          "TaskId": "3a24497b-e88d-7cc7-3e32-f17333abf8f3",
          "User": "K1A-23-039",
          "VehicleId": "323bf1d5-0e81-5ba0-a276-5a57648ed686"
        },
        "body": "XT K1A-SP26104000 - [RecordDeliveryArrival] arrival=3a244a99-4bac-e1cd-b8fe-67d5f68dcb42 task=3a24497b-e88d-7cc7-3e32-f17333abf8f3 order=WBJIL266D765215206 vehicle=323bf1d5-0e81-5ba0-a276-5a57648ed686 distance=376m gpsValid=False superseded=null",
        "id": "0m7N5sfJOYik5sUUB0UxjT0XUtx",
        "span_id": "bf5328afdb4bc312",
        "trace_id": "9f6e8c2287252e718fb8937c9295d90b"
      },
      "timestamp": "2026-10-14T10:19:54.0308367+07:00"
    }
    """;

    [Fact]
    public void ToOrderLocationEntry_ReadsFullPrecisionLatLngFromBody()
    {
        OrderLocationLogEntry? entry = SignozProQueryService.ToOrderLocationEntry(Parse(OrderLocationRowJson));

        entry.Should().NotBeNull();
        entry!.OrderId.Should().Be("90065787968");
        entry.Month.Should().Be(new DateOnly(2026, 10, 1));
        entry.Lat.Should().Be(21.18257600000004m);
        entry.Lng.Should().Be(106.07455085300006m);
        entry.Source.Should().Be("PMS");
    }

    [Fact]
    public void ToOrderLocationEntry_FallsBackToAttributesWhenBodyHasNoCoordinates()
    {
        LogRow row = Parse(OrderLocationRowJson);
        row.Data!.Body = string.Empty;

        OrderLocationLogEntry? entry = SignozProQueryService.ToOrderLocationEntry(row);

        entry!.Lat.Should().Be(21.183m);
        entry.Lng.Should().Be(106.075m);
    }

    [Fact]
    public void ToDeliveryArrivalEntry_ReadsDeviceLatLngFromAttributes()
    {
        DeliveryArrivalLogEntry? entry = SignozProQueryService.ToDeliveryArrivalEntry(Parse(ArrivalRowJson));

        entry!.OrderId.Should().Be("WBJIL266D765215206");
        entry.IsGpsValid.Should().BeFalse();
        entry.DistanceMeters.Should().Be(376m);
        entry.DeviceLat.Should().Be(21.231m);
        entry.DeviceLng.Should().Be(105.592m);
    }

    [Fact]
    public void ToDeliveryArrivalEntry_LeavesDeviceLatLngNullWhenMissing()
    {
        LogRow row = Parse(ArrivalRowJson);
        row.Data!.AttributesNumber.Remove("Lat");
        row.Data.AttributesNumber.Remove("Lng");

        DeliveryArrivalLogEntry? entry = SignozProQueryService.ToDeliveryArrivalEntry(row);

        entry!.DeviceLat.Should().BeNull();
        entry.DeviceLng.Should().BeNull();
    }

    private static LogRow Parse(string json) => JsonSerializer.Deserialize<LogRow>(json)!;
}
