using TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.OrderStep1;

public class TimeRangeChunkerTests
{
    [Fact]
    public void Split_ShouldReturnSingleChunk_WhenRangeIsWithin60Minutes()
    {
        DateTimeOffset start = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMinutes(30);

        List<(DateTimeOffset Start, DateTimeOffset End)> chunks = TimeRangeChunker.Split(start, end).ToList();

        chunks.Should().ContainSingle();
        chunks[0].Start.Should().Be(start);
        chunks[0].End.Should().Be(end);
    }

    [Fact]
    public void Split_ShouldSplitIntoConsecutive60MinuteChunks_WhenRangeIsLonger()
    {
        DateTimeOffset start = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset end = start.AddMinutes(150);

        List<(DateTimeOffset Start, DateTimeOffset End)> chunks = TimeRangeChunker.Split(start, end).ToList();

        chunks.Should().HaveCount(3);
        chunks[0].Should().Be((start, start.AddMinutes(60)));
        chunks[1].Should().Be((start.AddMinutes(60), start.AddMinutes(120)));
        chunks[2].Should().Be((start.AddMinutes(120), end));
    }

    [Fact]
    public void Split_ShouldReturnNoChunks_WhenStartIsNotBeforeEnd()
    {
        DateTimeOffset start = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

        List<(DateTimeOffset Start, DateTimeOffset End)> chunks = TimeRangeChunker.Split(start, start).ToList();

        chunks.Should().BeEmpty();
    }
}
