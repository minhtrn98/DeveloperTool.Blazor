using StackExchange.Redis;
using System.Collections.Concurrent;
using TMS.DeveloperTool.Blazor.Models;

namespace TMS.DeveloperTool.Blazor.Services;

public sealed class RequestChangeDriverMonitorService : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ConcurrentDictionary<string, RequestChangeDriverData> _cacheItems;
    private readonly ConcurrentBag<EventHandler> _eventRegisters;
    private readonly DriverService _driverService;
    private readonly FleetService _fleetService;

    public RequestChangeDriverMonitorService(IConnectionMultiplexer redis, DriverService driverService, FleetService fleetService)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
        _cacheItems = [];
        _eventRegisters = [];
        _driverService = driverService;
        _fleetService = fleetService;
    }

    public void RegisterEventHandler(EventHandler handler)
    {
        _eventRegisters.Add(handler);
    }

    public void UnregisterEventHandler(EventHandler handler)
    {
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
            Console.WriteLine($"Error deleting cache item {key}: {ex.Message}");
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
            Dictionary<Guid, string> driverDic = await _driverService.GetDriverNamesAsync(driverIds, CancellationToken.None);

            List<Guid> vehicleIds = _cacheItems.Values.Where(x => x.VehicleLicensePlate is null)
                .Select(x => x.VehicleId)
                .Distinct()
                .ToList();
            Dictionary<Guid, string> vehicleDic = await _fleetService.GetVehiclePlateAsync(vehicleIds, CancellationToken.None);

            foreach (RequestChangeDriverData data in _cacheItems.Values)
            {
                data.DriverRequestName = driverDic.GetValueOrDefault(data.DriverRequest) ?? data.DriverRequestName;
                data.VehicleLicensePlate = vehicleDic.GetValueOrDefault(data.VehicleId) ?? data.VehicleLicensePlate;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading initial cache items: {ex.Message}");
        }
    }

    private RedisCacheItem? GetCacheItem(string key)
    {
        try
        {
            if (!_db.KeyExists(key))
                return null;

            var type = _db.KeyType(key);
            var ttl = _db.KeyTimeToLive(key);
            string value;

            switch (type)
            {
                case RedisType.String:
                    value = _db.StringGet(key).ToString();
                    break;
                case RedisType.Hash:
                    var hashEntries = _db.HashGetAll(key);
                    value = string.Join(", ", hashEntries.Select(h => $"{h.Name}: {h.Value}"));
                    break;
                case RedisType.List:
                    var listItems = _db.ListRange(key, 0, 10);
                    value = string.Join(", ", listItems.Take(10).Select(v => v.ToString()));
                    if (listItems.Length > 10) value += "...";
                    break;
                case RedisType.Set:
                    var setMembers = _db.SetMembers(key);
                    value = string.Join(", ", setMembers.Take(10).Select(v => v.ToString()));
                    if (setMembers.Length > 10) value += "...";
                    break;
                case RedisType.SortedSet:
                    var sortedSetMembers = _db.SortedSetRangeByRankWithScores(key, 0, 10);
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
            Console.WriteLine($"Error getting cache item {key}: {ex.Message}");
            return null;
        }
    }

    private void InvokeEventHandlers()
    {
        foreach (EventHandler handler in _eventRegisters)
        {
            handler.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}