using Dapper;
using Npgsql;
using TMS.DeveloperTool.Blazor.Features.Dashboard.Models;
using TMS.DeveloperTool.Blazor.Features.Report.Models;

namespace TMS.DeveloperTool.Blazor.Features.Report.Services;

/// <summary>
/// Builds the <c>report.daily_*</c> rows for one Vietnam calendar day from the pro.*_logs tables,
/// in a single transaction that deletes and rebuilds the whole day (so re-running is safe).
/// The same SQL can target <c>pg_temp</c> copies of those tables (<see cref="AggregateLiveAsync"/>)
/// so the dashboard computes days not yet in the report (e.g. today) with identical semantics.
/// </summary>
public sealed class DailyReportAggregator(ConnectionStringsOptions connectionStrings)
{
    /// <summary>
    /// Bump when the aggregation logic, a histogram edge set or the meaning of a column changes:
    /// <see cref="DailyReportJob"/> then rebuilds every day produced by an older version.
    /// </summary>
    public const int AggregationVersion = 2;

    /// <summary>How long after a manifest's creation day check-ins / completions still count toward its lifecycle.</summary>
    public static readonly TimeSpan ManifestLookAhead = TimeSpan.FromDays(2);

    /// <summary>Histogram edges in seconds; a superset of the dashboard's coarser duration buckets.</summary>
    public static readonly double[] DurationEdgesSeconds =
    [
        30, 60, 120, 300, 600, 900, 1200, 1800, 2700, 3600, 5400, 7200, 10800, 14400, 18000,
        21600, 28800, 36000, 43200, 57600, 72000, 86400, 129600, 172800
    ];

    /// <summary>Histogram edges in meters; a superset of the dashboard's distance buckets.</summary>
    public static readonly double[] DistanceEdgesMeters = [10, 20, 50, 100, 200, 300, 500, 1000, 2000, 5000, 10000];

    // Transaction-level advisory lock: two instances never rebuild the same tables concurrently.
    private const long AggregationLockKey = 7_261_002;

    private const int CommandTimeoutSeconds = 600;

    private const string InDay = "log_timestamp >= @DayStart AND log_timestamp < @DayEnd";

    private static readonly string DaySqlTemplate = $$"""
        DELETE FROM {target}.daily_event_stats WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_event_time_buckets WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_actor_hour_stats WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_label_counts WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_histograms WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_handover_stats WHERE report_date = @ReportDate::date;
        DELETE FROM {target}.daily_manifest_stats WHERE report_date = @ReportDate::date;

        CREATE TEMP TABLE tmp_report_events AS
        SELECT e.event_type,
               e.log_timestamp,
               e.log_timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh' AS local_ts,
               btrim(e.actor) AS actor,
               e.quantity::bigint AS quantity,
               e.amount::numeric(18, 2) AS amount
        FROM (
            SELECT '{{DashboardEventType.ManifestCommit}}' AS event_type, log_timestamp, actor, 0 AS quantity, 0::numeric AS amount
            FROM pro.delivery_manifest_commit_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.SessionCommit}}', log_timestamp, actor, item_count, 0 FROM pro.delivery_session_commit_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.Arrival}}', log_timestamp, actor, 0, 0 FROM pro.delivery_arrival_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.TaskComplete}}', log_timestamp, actor, delivered_count, collected_cod FROM pro.delivery_task_complete_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.Failure}}', log_timestamp, actor, item_count, 0 FROM pro.delivery_failure_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.Transfer}}', log_timestamp, actor, order_count, 0 FROM pro.delivery_transfer_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{DashboardEventType.UnloadingHandover}}', log_timestamp, actor, item_count, 0 FROM pro.unloading_handover_logs WHERE {{InDay}}
        ) e;

        INSERT INTO {target}.daily_event_stats (report_date, event_type, event_count, quantity, amount, distinct_actors)
        SELECT @ReportDate::date, event_type, count(*), sum(quantity), sum(amount), count(DISTINCT actor) FILTER (WHERE actor <> '')
        FROM tmp_report_events
        GROUP BY event_type;

        INSERT INTO {target}.daily_event_time_buckets (report_date, event_type, minute_of_day, event_count)
        SELECT @ReportDate::date, event_type, minute_of_day, count(*)
        FROM (
            SELECT event_type, ((extract(hour FROM local_ts)::int * 60 + extract(minute FROM local_ts)::int) / 10 * 10)::smallint AS minute_of_day
            FROM tmp_report_events
        ) t
        GROUP BY event_type, minute_of_day;

        INSERT INTO {target}.daily_actor_hour_stats (report_date, event_type, actor, hour, event_count, quantity, amount, first_at, last_at)
        SELECT @ReportDate::date, event_type, actor, hour, count(*), sum(quantity), sum(amount), min(log_timestamp), max(log_timestamp)
        FROM (SELECT *, extract(hour FROM local_ts)::smallint AS hour FROM tmp_report_events) t
        GROUP BY event_type, actor, hour;

        INSERT INTO {target}.daily_label_counts (report_date, metric, label, event_count)
        SELECT @ReportDate::date, metric, label, count(*)
        FROM (
            SELECT '{{ReportMetric.FailureType}}' AS metric, btrim(failure_type) AS label FROM pro.delivery_failure_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{ReportMetric.TransferSource}}', btrim(source_type) FROM pro.delivery_transfer_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{ReportMetric.ArrivalGps}}', CASE is_gps_valid WHEN TRUE THEN 'valid' WHEN FALSE THEN 'invalid' ELSE 'unknown' END
            FROM pro.delivery_arrival_logs WHERE {{InDay}}
            UNION ALL
            SELECT '{{ReportMetric.ArrivalSuperseded}}', CASE WHEN superseded <> 0 THEN 'superseded' ELSE 'first' END
            FROM pro.delivery_arrival_logs WHERE {{InDay}}
        ) t
        GROUP BY metric, label;

        INSERT INTO {target}.daily_handover_stats (report_date, created_count, item_count, received_count, confirmed_count, handled_count)
        SELECT @ReportDate::date, count(*), coalesce(sum(item_count), 0), count(received_at), count(confirm_at),
               count(*) FILTER (WHERE received_at IS NOT NULL OR confirm_at IS NOT NULL)
        FROM pro.unloading_handover_logs WHERE {{InDay}};

        -- Manifests created this day (commit + session commit) joined to their first check-in and
        -- last completion within the look-ahead window. The @DayStart lower bound lets the
        -- log_timestamp indexes narrow the arrival / completion scans.
        CREATE TEMP TABLE tmp_report_manifests AS
        WITH created AS (
            SELECT delivery_manifest_code AS code, log_timestamp FROM pro.delivery_manifest_commit_logs WHERE {{InDay}}
            UNION ALL
            SELECT unnest(manifest_codes), log_timestamp FROM pro.delivery_session_commit_logs WHERE {{InDay}}
        ), manifests AS (
            SELECT code, min(log_timestamp) AS committed_at FROM created WHERE code <> '' GROUP BY code
        ), arrivals AS (
            SELECT m.code, min(a.log_timestamp) AS first_arrival_at
            FROM manifests m
            JOIN pro.delivery_arrival_logs a ON a.delivery_manifest_code = m.code
            WHERE a.log_timestamp >= @DayStart AND a.log_timestamp < @LookAheadEnd AND a.log_timestamp >= m.committed_at
            GROUP BY m.code
        ), completes AS (
            SELECT m.code, max(c.log_timestamp) AS last_complete_at
            FROM manifests m
            JOIN pro.delivery_task_complete_logs c ON c.delivery_manifest_code = m.code
            WHERE c.log_timestamp >= @DayStart AND c.log_timestamp < @LookAheadEnd AND c.log_timestamp >= m.committed_at
            GROUP BY m.code
        )
        SELECT m.code, m.committed_at, a.first_arrival_at, c.last_complete_at
        FROM manifests m
        LEFT JOIN arrivals a USING (code)
        LEFT JOIN completes c USING (code);

        INSERT INTO {target}.daily_manifest_stats (report_date, created_count, with_arrival_count, with_complete_count, idle_count)
        SELECT @ReportDate::date, count(*), count(first_arrival_at), count(last_complete_at),
               count(*) FILTER (WHERE first_arrival_at IS NULL AND last_complete_at IS NULL)
        FROM tmp_report_manifests;

        INSERT INTO {target}.daily_histograms (report_date, metric, bucket_index, bucket_lower, sample_count, value_sum, value_max)
        SELECT @ReportDate::date, metric, bucket_index, CASE WHEN bucket_index = 0 THEN 0 ELSE edges[bucket_index] END, count(*), sum(value), max(value)
        FROM (
            SELECT metric, value, edges, width_bucket(value, edges)::smallint AS bucket_index
            FROM (
                SELECT '{{ReportMetric.ArrivalDistance}}' AS metric, distance_meters::float8 AS value, @DistanceEdges::float8[] AS edges
                FROM pro.delivery_arrival_logs WHERE {{InDay}} AND distance_meters IS NOT NULL
                UNION ALL
                SELECT '{{ReportMetric.HandoverReceive}}', extract(epoch FROM received_at - log_timestamp)::float8, @DurationEdges::float8[]
                FROM pro.unloading_handover_logs WHERE {{InDay}} AND received_at >= log_timestamp
                UNION ALL
                SELECT '{{ReportMetric.HandoverConfirm}}', extract(epoch FROM confirm_at - log_timestamp)::float8, @DurationEdges::float8[]
                FROM pro.unloading_handover_logs WHERE {{InDay}} AND confirm_at >= log_timestamp
                UNION ALL
                SELECT '{{ReportMetric.HandoverFirstHandled}}', extract(epoch FROM least(received_at, confirm_at) - log_timestamp)::float8, @DurationEdges::float8[]
                FROM pro.unloading_handover_logs WHERE {{InDay}} AND least(received_at, confirm_at) >= log_timestamp
                UNION ALL
                SELECT '{{ReportMetric.ManifestFirstArrival}}', extract(epoch FROM first_arrival_at - committed_at)::float8, @DurationEdges::float8[]
                FROM tmp_report_manifests WHERE first_arrival_at IS NOT NULL
                UNION ALL
                SELECT '{{ReportMetric.ManifestLastComplete}}', extract(epoch FROM last_complete_at - committed_at)::float8, @DurationEdges::float8[]
                FROM tmp_report_manifests WHERE last_complete_at IS NOT NULL
            ) samples
        ) bucketed
        GROUP BY metric, bucket_index, edges;

        DROP TABLE tmp_report_manifests;
        DROP TABLE tmp_report_events;
        """;

    // The run is recorded after the day's SQL, from the event count it inserted.
    private static readonly string RecordRunSql = $$"""
        INSERT INTO report.aggregation_runs (report_date, aggregation_version, status, event_count, started_at, finished_at, error)
        VALUES (@ReportDate::date, @AggregationVersion, '{{AggregationRunStatus.Succeeded}}', @EventCount, @StartedAt, clock_timestamp(), NULL)
        ON CONFLICT (report_date) DO UPDATE SET
            aggregation_version = EXCLUDED.aggregation_version,
            status = EXCLUDED.status,
            event_count = EXCLUDED.event_count,
            started_at = EXCLUDED.started_at,
            finished_at = EXCLUDED.finished_at,
            error = NULL;
        """;

    /// <summary>
    /// The per-day aggregation SQL writing into <paramref name="target"/> ("report", or "pg_temp"
    /// after <see cref="CreateLiveTablesSql"/>). Temp work tables are dropped at the end so several
    /// days can run in one transaction.
    /// </summary>
    private static string DaySql(string target) => DaySqlTemplate.Replace("{target}", target);

    // pg_temp copies of the daily tables for AggregateLiveAsync; dropped with the transaction.
    private static readonly string CreateLiveTablesSql = string.Join(Environment.NewLine, ReportReader.DailyTables.Select(
        table => $"CREATE TEMP TABLE {table} (LIKE report.{table} INCLUDING DEFAULTS) ON COMMIT DROP;"));

    /// <summary>Rebuilds one day; returns the number of source events aggregated.</summary>
    public async Task<int> AggregateDayAsync(DateOnly reportDate, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;

        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            $"SELECT pg_advisory_xact_lock({AggregationLockKey})", transaction: transaction, cancellationToken: cancellationToken));

        await ExecuteDaySqlAsync(connection, transaction, "report", reportDate, cancellationToken);

        int eventCount = await connection.QuerySingleAsync<int>(new CommandDefinition(
            "SELECT coalesce(sum(event_count), 0)::int FROM report.daily_event_stats WHERE report_date = @ReportDate::date",
            new { ReportDate = ToDbDate(reportDate) },
            transaction,
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            RecordRunSql,
            new { ReportDate = ToDbDate(reportDate), AggregationVersion, EventCount = eventCount, StartedAt = startedAt },
            transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return eventCount;
    }

    /// <summary>
    /// Runs the report aggregation for <paramref name="dates"/> into pg_temp copies of the daily
    /// tables and reads them back — nothing is written to the report schema. For days the job
    /// hasn't aggregated yet (today, or a missing/failed/outdated day).
    /// </summary>
    public async Task<ReportDataSet> AggregateLiveAsync(IReadOnlyCollection<DateOnly> dates, CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            return ReportDataSet.Empty;
        }

        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(CreateLiveTablesSql, transaction: transaction, cancellationToken: cancellationToken));
        foreach (DateOnly date in dates)
        {
            await ExecuteDaySqlAsync(connection, transaction, "pg_temp", date, cancellationToken);
        }

        ReportDataSet data = await ReportReader.ReadAsync(connection, transaction, "pg_temp", dates, cancellationToken);
        await transaction.RollbackAsync(cancellationToken);
        return data;
    }

    private static Task ExecuteDaySqlAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string target, DateOnly reportDate, CancellationToken cancellationToken)
    {
        (DateTimeOffset dayStart, DateTimeOffset dayEnd) = DailyReportSchedule.DayBoundsUtc(reportDate);
        return connection.ExecuteAsync(new CommandDefinition(
            DaySql(target),
            new
            {
                ReportDate = ToDbDate(reportDate),
                DayStart = dayStart,
                DayEnd = dayEnd,
                LookAheadEnd = dayEnd + ManifestLookAhead,
                DistanceEdges = DistanceEdgesMeters,
                DurationEdges = DurationEdgesSeconds
            },
            transaction,
            CommandTimeoutSeconds,
            cancellationToken: cancellationToken));
    }

    /// <summary>Records a failed day outside the (rolled back) aggregation transaction.</summary>
    public async Task MarkFailedAsync(DateOnly reportDate, string error, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        await connection.ExecuteAsync(new CommandDefinition(
            $"""
            INSERT INTO report.aggregation_runs (report_date, aggregation_version, status, started_at, finished_at, error)
            VALUES (@ReportDate::date, @AggregationVersion, '{AggregationRunStatus.Failed}', now(), now(), @Error)
            ON CONFLICT (report_date) DO UPDATE SET
                aggregation_version = EXCLUDED.aggregation_version,
                status = EXCLUDED.status,
                finished_at = EXCLUDED.finished_at,
                error = EXCLUDED.error
            """,
            new { ReportDate = ToDbDate(reportDate), AggregationVersion, Error = error },
            cancellationToken: cancellationToken));
    }

    public async Task<Dictionary<DateOnly, AggregationRunInfo>> GetRunsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        IEnumerable<RunRow> rows = await connection.QueryAsync<RunRow>(new CommandDefinition(
            """
            SELECT report_date AS ReportDate, aggregation_version AS AggregationVersion, status AS Status, finished_at AS FinishedAt
            FROM report.aggregation_runs
            WHERE report_date BETWEEN @From::date AND @To::date
            """,
            new { From = ToDbDate(from), To = ToDbDate(to) },
            cancellationToken: cancellationToken));

        return rows
            .Select(x => new AggregationRunInfo(
                x.ReportDate,
                x.AggregationVersion,
                x.Status,
                x.FinishedAt is { } finishedAt ? new DateTimeOffset(DateTime.SpecifyKind(finishedAt, DateTimeKind.Utc)) : null))
            .ToDictionary(x => x.ReportDate);
    }

    /// <summary>Vietnam date of the oldest row across the aggregated pro.*_logs tables, or null when all are empty.</summary>
    public async Task<DateOnly?> GetEarliestLogDateAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionStrings.DeveloperDb);
        DateTime? earliest = await connection.ExecuteScalarAsync<DateTime?>(new CommandDefinition(
            """
            SELECT min(ts) FROM (
                SELECT min(log_timestamp) AS ts FROM pro.delivery_manifest_commit_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.delivery_session_commit_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.delivery_arrival_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.delivery_task_complete_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.delivery_failure_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.delivery_transfer_logs
                UNION ALL SELECT min(log_timestamp) FROM pro.unloading_handover_logs
            ) t
            """,
            commandTimeout: CommandTimeoutSeconds,
            cancellationToken: cancellationToken));

        return earliest is { } value
            ? DailyReportSchedule.ToVietnamDate(new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
            : null;
    }

    // Dapper can't bind DateOnly; pass a DateTime and cast with ::date in SQL.
    internal static DateTime ToDbDate(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);

    private sealed class RunRow
    {
        public DateOnly ReportDate { get; init; }
        public int AggregationVersion { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime? FinishedAt { get; init; }
    }
}
