using Dapper;
using Npgsql;
using TMS.DeveloperTool.Blazor.Features.Report.Models;

namespace TMS.DeveloperTool.Blazor.Features.Report.Services;

/// <summary>
/// Reads the report.daily_* tables — from the <c>report</c> schema, or from the pg_temp copies
/// <see cref="DailyReportAggregator.AggregateLiveAsync"/> fills — into a <see cref="ReportDataSet"/>.
/// </summary>
public static class ReportReader
{
    public static readonly string[] DailyTables =
    [
        "daily_event_stats",
        "daily_event_time_buckets",
        "daily_actor_hour_stats",
        "daily_label_counts",
        "daily_histograms",
        "daily_handover_stats",
        "daily_manifest_stats"
    ];

    private const string InDates = "report_date = ANY(@Dates::date[])";

    public static async Task<ReportDataSet> ReadAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction? transaction,
        string schema,
        IReadOnlyCollection<DateOnly> dates,
        CancellationToken cancellationToken)
    {
        if (dates.Count == 0)
        {
            return ReportDataSet.Empty;
        }

        string sql = $"""
            SELECT report_date AS Date, event_type AS EventType, event_count AS Count, quantity AS Quantity, amount AS Amount
            FROM {schema}.daily_event_stats WHERE {InDates};

            SELECT report_date AS Date, event_type AS EventType, minute_of_day::int AS MinuteOfDay, event_count AS Count
            FROM {schema}.daily_event_time_buckets WHERE {InDates};

            SELECT report_date AS Date, event_type AS EventType, actor AS Actor, hour::int AS Hour, event_count AS Count,
                   quantity AS Quantity, amount AS Amount, first_at AS FirstAtUtc, last_at AS LastAtUtc
            FROM {schema}.daily_actor_hour_stats WHERE {InDates};

            SELECT report_date AS Date, metric AS Metric, label AS Label, event_count AS Count
            FROM {schema}.daily_label_counts WHERE {InDates};

            SELECT report_date AS Date, metric AS Metric, bucket_index::int AS BucketIndex, bucket_lower AS BucketLower,
                   sample_count AS Count, value_sum AS ValueSum, value_max AS ValueMax
            FROM {schema}.daily_histograms WHERE {InDates};

            SELECT report_date AS Date, created_count AS Created, item_count AS Items, received_count AS Received,
                   confirmed_count AS Confirmed, handled_count AS Handled
            FROM {schema}.daily_handover_stats WHERE {InDates};

            SELECT report_date AS Date, created_count AS Created, with_arrival_count AS WithArrival,
                   with_complete_count AS WithComplete, idle_count AS Idle
            FROM {schema}.daily_manifest_stats WHERE {InDates};
            """;

        await using SqlMapper.GridReader grid = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new { Dates = dates.Select(DailyReportAggregator.ToDbDate).ToArray() },
            transaction,
            commandTimeout: 300,
            cancellationToken: cancellationToken));

        return new ReportDataSet(
            (await grid.ReadAsync<EventStatRow>()).ToList(),
            (await grid.ReadAsync<TimeBucketRow>()).ToList(),
            (await grid.ReadAsync<ActorHourRow>()).ToList(),
            (await grid.ReadAsync<LabelCountRow>()).ToList(),
            (await grid.ReadAsync<HistogramRow>()).ToList(),
            (await grid.ReadAsync<HandoverDayRow>()).ToList(),
            (await grid.ReadAsync<ManifestDayRow>()).ToList());
    }
}
