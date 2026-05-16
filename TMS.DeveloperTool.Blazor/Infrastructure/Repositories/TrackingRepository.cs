namespace TMS.DeveloperTool.Blazor.Infrastructure.Repositories;

public sealed class TrackingRepository([FromKeyedServices("TrackingDb")] ApplicationDbQuery dbQuery)
{
    public IAsyncEnumerable<TopMovingVehicleDto> GetTopMovingVehiclesAsync()
    {
        const string sql = """
            WITH recent AS (
                SELECT
                    vs."ActualPlate",
                    vs."Plate",
                    vs."GpsTime",
                    vs."Speed",
                    vs."LastOdoMile",
                    LAG(vs."LastOdoMile") OVER (
                        PARTITION BY vs."ActualPlate"
                        ORDER BY vs."GpsTime"
                    ) AS prev_odo
                FROM public."VehicleStatuses" vs
                WHERE vs."GpsTime" >= now() - interval '1 minute' and vs."GpsTime" <= now() + interval '2 hour'
            )
            SELECT
                r."ActualPlate",
                r."Plate",
                MAX(r."GpsTime") AS "LastTime",
                MAX(r."Speed") AS "MaxSpeed",
                COALESCE(MAX(r."LastOdoMile") - MIN(r."LastOdoMile"), 0) AS "OdoDiff",
                COUNT(*) AS "GpsPoints",
                (
                    MAX(r."Speed") * 2 +
                    COALESCE(MAX(r."LastOdoMile") - MIN(r."LastOdoMile"), 0) * 10 +
                    COUNT(*)
                ) AS "MovingScore"
            FROM recent r
            GROUP BY r."ActualPlate", r."Plate"
            HAVING
                MAX(r."Speed") > 0
                OR (MAX(r."LastOdoMile") - MIN(r."LastOdoMile")) > 0
            ORDER BY "MovingScore" DESC
            """;

        return dbQuery.QueryUnbufferedAsync<TopMovingVehicleDto>(sql);
    }

}
