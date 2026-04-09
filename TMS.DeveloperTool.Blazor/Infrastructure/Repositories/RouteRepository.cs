using TMS.DeveloperTool.Blazor.Features.Routing.Models;

namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class RouteRepository
{
    private readonly ApplicationDbQuery _dbQuery;
    private readonly CacheService _cacheService;

    public RouteRepository([FromKeyedServices("RouteDb")] ApplicationDbQuery dbQuery, CacheService cacheService)
    {
        _dbQuery = dbQuery;
        _cacheService = cacheService;
    }

    // get "PostOffices" table data
    public async Task<IEnumerable<PostOffice>> GetPostOfficesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<PostOffice>? cache = _cacheService.GetPostOfficesCache();
        if (cache != null)
        {
            return cache;
        }

        const string sql = """
            SELECT "Id", "PostOfficeCode", "PostOfficeName", "StreetAddress", "Longitude", "Latitude", p."WardId", p."ProvinceId", "Wards"."WardName" AS "WardName", "Provinces"."ProvinceName" AS "ProvinceName"
            FROM public."PostOffices" p
            join public."Wards" on p."WardId" = "Wards"."WardId"
            join public."Provinces" on p."ProvinceId" = "Provinces"."ProvinceId"
            WHERE "Longitude" IS NOT NULL AND "Latitude" IS NOT NULL
        """;
        IEnumerable<PostOffice> records = await _dbQuery.QueryAsync<PostOffice>(sql, null, cancellationToken);
        _cacheService.SetPostOfficesCache(records);

        return records;
    }
}