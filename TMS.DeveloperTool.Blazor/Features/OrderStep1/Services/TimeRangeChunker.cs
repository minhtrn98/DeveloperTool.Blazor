namespace TMS.DeveloperTool.Blazor.Features.OrderStep1.Services;

public static class TimeRangeChunker
{
    private static readonly TimeSpan MaxChunkSize = TimeSpan.FromMinutes(60);

    public static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Split(DateTimeOffset start, DateTimeOffset end)
    {
        DateTimeOffset chunkStart = start;
        while (chunkStart < end)
        {
            DateTimeOffset chunkEnd = chunkStart + MaxChunkSize < end ? chunkStart + MaxChunkSize : end;
            yield return (chunkStart, chunkEnd);
            chunkStart = chunkEnd;
        }
    }
}
