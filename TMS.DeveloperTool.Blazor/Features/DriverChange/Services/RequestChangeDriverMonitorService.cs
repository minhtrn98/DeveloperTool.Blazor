using StackExchange.Redis;
using System.Collections.Concurrent;
using TMS.DeveloperTool.Blazor.Features.DriverChange.Models;

namespace TMS.DeveloperTool.Blazor.Features.DriverChange.Services;

public sealed class RequestChangeDriverMonitorService : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ConcurrentDictionary<string, RequestChangeDriverData> _cacheItems;
    private readonly ConcurrentDictionary<Guid, EventHandler> _eventRegisters;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RequestChangeDriverMonitorService> _logger;

    public RequestChangeDriverMonitorService(IConnectionMultiplexer redis, IServiceProvider serviceProvider, ILogger<RequestChangeDriverMonitorService> logger)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _cacheItems = [];
        _eventRegisters = [];
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Guid RegisterEventHandler(EventHandler handler)
    {
        Guid key = Guid.CreateVersion7();
        _eventRegisters[key] = handler;
        return key;
    }

    public void UnregisterEventHandler(Guid key)
    {
        _eventRegisters.TryRemove(key, out _);
    }

    public List<RequestChangeDriverData> GetAllCacheItems()
    {
        return _cacheItems.Values.OrderBy(x => x.Key).ToList();
    }

    public async Task RefreshCacheItem()
    {
        _cacheItems.Clear();
        await LoadInitialCacheItems();

        InvokeEventHandlers();
    }

    public void DeleteCacheItem(string key)
    {
        try
        {
            _db.KeyDelete(key);
            _cacheItems.TryRemove(key, out _);

            InvokeEventHandlers();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cache item {Key}", key);
        }
    }

    private async Task LoadInitialCacheItems()
    {
        try
        {
            const string pattern = "Pairing:*";
            IServer server = _redis.GetServer(_redis.GetEndPoints()[0]);
            IEnumerable<RedisKey> keys = server.Keys(pattern: pattern, pageSize: 1000);

            foreach (RedisKey key in keys)
            {
                RedisCacheItem? item = GetCacheItem(key.ToString());
                if (item != null)
                {
                    RequestChangeDriverData? data = System.Text.Json.JsonSerializer.Deserialize<RequestChangeDriverData>(item.Value);
                    if (data != null)
                    {
                        string keyStr = key.ToString();
                        data.Key = keyStr;
                        _cacheItems[keyStr] = data;
                    }
                }
            }

            List<Guid> driverIds = _cacheItems.Values.Where(x => x.DriverRequestName is null)
                .Select(x => x.DriverRequest)
                .Distinct()
                .ToList();
            List<Guid> vehicleIds = _cacheItems.Values.Where(x => x.VehicleLicensePlate is null)
                .Select(x => x.VehicleId)
                .Distinct()
                .ToList();

            if (driverIds.Count > 0 || vehicleIds.Count > 0)
            {
                using IServiceScope scope = _serviceProvider.CreateScope();

                Dictionary<Guid, string> driverDic = [];
                Dictionary<Guid, string> vehicleDic = [];

                if (driverIds.Count > 0)
                {
                    DriverRepository driverService = scope.ServiceProvider.GetRequiredService<DriverRepository>();
                    driverDic = await driverService.GetDriverNamesAsync(driverIds, CancellationToken.None);
                }
                if (driverIds.Count > 0)
                {
                    FleetRepository fleetService = scope.ServiceProvider.GetRequiredService<FleetRepository>();
                    vehicleDic = await fleetService.GetVehiclePlateAsync(vehicleIds, CancellationToken.None);
                }

                foreach (RequestChangeDriverData data in _cacheItems.Values)
                {
                    data.DriverRequestName = driverDic.GetValueOrDefault(data.DriverRequest) ?? data.DriverRequestName;
                    data.VehicleLicensePlate = vehicleDic.GetValueOrDefault(data.VehicleId) ?? data.VehicleLicensePlate;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading initial cache items");
        }
    }

    private RedisCacheItem? GetCacheItem(string key)
    {
        try
        {
            if (!_db.KeyExists(key))
                return null;

            RedisType type = _db.KeyType(key);
            TimeSpan? ttl = _db.KeyTimeToLive(key);
            string value;

            switch (type)
            {
                case RedisType.String:
                    value = _db.StringGet(key).ToString();
                    break;
                case RedisType.Hash:
                    HashEntry[] hashEntries = _db.HashGetAll(key);
                    value = string.Join(", ", hashEntries.Select(h => $"{h.Name}: {h.Value}"));
                    break;
                case RedisType.List:
                    RedisValue[] listItems = _db.ListRange(key, 0, 10);
                    value = string.Join(", ", listItems.Take(10).Select(v => v.ToString()));
                    if (listItems.Length > 10) value += "...";
                    break;
                case RedisType.Set:
                    RedisValue[] setMembers = _db.SetMembers(key);
                    value = string.Join(", ", setMembers.Take(10).Select(v => v.ToString()));
                    if (setMembers.Length > 10) value += "...";
                    break;
                case RedisType.SortedSet:
                    SortedSetEntry[] sortedSetMembers = _db.SortedSetRangeByRankWithScores(key, 0, 10);
                    value = string.Join(", ", sortedSetMembers.Take(10).Select(v => $"{v.Element}:{v.Score}"));
                    if (sortedSetMembers.Length > 10) value += "...";
                    break;
                default:
                    value = $"[{type}]";
                    break;
            }

            return new RedisCacheItem
            {
                Key = key,
                Value = value,
                Type = type.ToString(),
                Ttl = ttl,
                LastModified = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache item {Key}", key);
            return null;
        }
    }

    private void InvokeEventHandlers()
    {
        foreach ((Guid _, EventHandler? handler) in _eventRegisters)
        {
            handler.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}