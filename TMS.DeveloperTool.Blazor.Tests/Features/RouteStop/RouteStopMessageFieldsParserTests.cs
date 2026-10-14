using TMS.DeveloperTool.Blazor.Features.RouteStop.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.RouteStop;

public class RouteStopMessageFieldsParserTests
{
    [Fact]
    public void ParseLocation_ReadsLatitudeAndLongitude()
    {
        const string detail = """
        { "EventType": "RouteStopRecorded", "Latitude": 10.765274999999999, "Longitude": 106.69476999999999, "ActionObjectId": "x" }
        """;

        (decimal? latitude, decimal? longitude) = RouteStopMessageFieldsParser.ParseLocation(detail);

        latitude.Should().Be(10.765274999999999m);
        longitude.Should().Be(106.69476999999999m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("""{ "Latitude": null, "Longitude": "106.1" }""")]
    public void ParseLocation_ReturnsNullsWhenMissingOrInvalid(string detail)
    {
        (decimal? latitude, decimal? longitude) = RouteStopMessageFieldsParser.ParseLocation(detail);

        latitude.Should().BeNull();
        longitude.Should().BeNull();
    }
}
