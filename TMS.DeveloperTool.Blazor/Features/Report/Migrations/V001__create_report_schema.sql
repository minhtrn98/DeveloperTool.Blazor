-- Pre-aggregated daily report data built from the pro.*_logs tables by DailyReportJob.
-- Every report_date is a Vietnam (Asia/Ho_Chi_Minh) calendar day. Rows for a day are deleted and
-- rebuilt as a whole on each aggregation, so all tables are safe to recompute.

CREATE SCHEMA IF NOT EXISTS report;

-- One row per aggregated day: which aggregation logic version produced it and when. The job
-- recomputes a day when it is missing, failed, older than DailyReportAggregator.AggregationVersion,
-- or was last built before the day "settled" (late logs / lifecycles still arriving).
CREATE TABLE IF NOT EXISTS report.aggregation_runs (
    report_date DATE PRIMARY KEY,
    aggregation_version INT NOT NULL,
    status TEXT NOT NULL,
    event_count INT NOT NULL DEFAULT 0,
    started_at TIMESTAMPTZ NOT NULL,
    finished_at TIMESTAMPTZ NULL,
    error TEXT NULL
);

-- Totals per event type (one row per synced log table).
CREATE TABLE IF NOT EXISTS report.daily_event_stats (
    report_date DATE NOT NULL,
    event_type TEXT NOT NULL,
    event_count INT NOT NULL,
    quantity BIGINT NOT NULL,
    amount NUMERIC(18, 2) NOT NULL,
    distinct_actors INT NOT NULL,
    PRIMARY KEY (report_date, event_type)
);

-- 10-minute time-of-day histogram per event type: hour profile, weekday x hour heatmap and
-- time-of-day percentiles (to 10-minute precision) over any date range.
CREATE TABLE IF NOT EXISTS report.daily_event_time_buckets (
    report_date DATE NOT NULL,
    event_type TEXT NOT NULL,
    minute_of_day SMALLINT NOT NULL,
    event_count INT NOT NULL,
    PRIMARY KEY (report_date, event_type, minute_of_day)
);

-- Per actor per hour; summing over a range gives count/quantity/amount, active days
-- (distinct report_date), first/last time and peak hour per actor. actor = '' when unknown.
CREATE TABLE IF NOT EXISTS report.daily_actor_hour_stats (
    report_date DATE NOT NULL,
    event_type TEXT NOT NULL,
    actor TEXT NOT NULL,
    hour SMALLINT NOT NULL,
    event_count INT NOT NULL,
    quantity BIGINT NOT NULL,
    amount NUMERIC(18, 2) NOT NULL,
    first_at TIMESTAMPTZ NOT NULL,
    last_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (report_date, event_type, actor, hour)
);

CREATE INDEX IF NOT EXISTS idx_report_daily_actor_hour_stats_actor ON report.daily_actor_hour_stats (event_type, actor, report_date);

-- Categorical counts: failure_type, transfer_source, arrival_gps (valid/invalid/unknown),
-- arrival_superseded (first/superseded).
CREATE TABLE IF NOT EXISTS report.daily_label_counts (
    report_date DATE NOT NULL,
    metric TEXT NOT NULL,
    label TEXT NOT NULL,
    event_count INT NOT NULL,
    PRIMARY KEY (report_date, metric, label)
);

-- Fine-grained histograms (edges defined in DailyReportAggregator, tied to the aggregation
-- version) so coarser dashboard buckets and approximate percentiles can be derived for any range.
-- bucket_index 0 = below the first edge; bucket_lower is the bucket's inclusive lower bound.
CREATE TABLE IF NOT EXISTS report.daily_histograms (
    report_date DATE NOT NULL,
    metric TEXT NOT NULL,
    bucket_index SMALLINT NOT NULL,
    bucket_lower DOUBLE PRECISION NOT NULL,
    sample_count INT NOT NULL,
    value_sum DOUBLE PRECISION NOT NULL,
    PRIMARY KEY (report_date, metric, bucket_index)
);

-- Unloading handovers created on report_date and how many were received / confirmed so far.
CREATE TABLE IF NOT EXISTS report.daily_handover_stats (
    report_date DATE PRIMARY KEY,
    created_count INT NOT NULL,
    item_count BIGINT NOT NULL,
    received_count INT NOT NULL,
    confirmed_count INT NOT NULL,
    handled_count INT NOT NULL
);

-- Delivery manifests created on report_date (commit + session commit) and whether they got a
-- check-in / task completion within the look-ahead window.
CREATE TABLE IF NOT EXISTS report.daily_manifest_stats (
    report_date DATE PRIMARY KEY,
    created_count INT NOT NULL,
    with_arrival_count INT NOT NULL,
    with_complete_count INT NOT NULL,
    idle_count INT NOT NULL
);

-- Source tables filtered by day during aggregation but missing a log_timestamp index.
CREATE INDEX IF NOT EXISTS idx_pro_delivery_manifest_commit_logs_log_timestamp ON pro.delivery_manifest_commit_logs (log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_transfer_logs_log_timestamp ON pro.delivery_transfer_logs (log_timestamp DESC);
