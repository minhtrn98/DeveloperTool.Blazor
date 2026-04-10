using System.Collections.Concurrent;
using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Caching;

public sealed class CacheService
{
    // Implement caching functionalities here using in-memory data structures with singleton lifetime.
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public void SetPostOfficesCache(IEnumerable<PostOffice> postOffices)
    {
        _cache["PostOffices"] = postOffices;
    }

    public void SetDepartmentsCache(IEnumerable<DepartmentDto> departmentDtos)
    {
        _cache["DepartmentDtos"] = departmentDtos;
    }

    public IEnumerable<PostOffice>? GetPostOfficesCache()
    {
        if (_cache.TryGetValue("PostOffices", out var cachedValue) && cachedValue is IEnumerable<PostOffice> postOffices)
        {
            return postOffices;
        }
        return null;
    }

    public IEnumerable<DepartmentDto>? GetDepartmentsCache()
    {
        if (_cache.TryGetValue("DepartmentDtos", out var cachedValue) && cachedValue is IEnumerable<DepartmentDto> departmentDtos)
        {
            return departmentDtos;
        }
        return null;
    }

    public void Set<T>(string key, T value)
    {
        _cache[key] = value!;
    }

    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}