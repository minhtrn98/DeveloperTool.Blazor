namespace TMS.DeveloperTool.Blazor.Infrastructure.Caching;

public sealed class MyRedisOptions
{
    public const string SectionName = "MyRedis";

    public required string ConnectionString { get; init; }
    public required string Password { get; init; }
    public required int Database { get; init; }
    public required int ConnectTimeout { get; init; }
    public required int SyncTimeout { get; init; }
    public required int AsyncTimeout { get; init; }
    public required int ConnectRetry { get; init; }
    public required bool AbortOnConnectFail { get; init; }
}
