-- Largest sample per histogram bucket: the dashboard's "Max" column and the upper bound used to
-- interpolate percentiles inside the open-ended top bucket. Existing rows get 0 until rebuilt;
-- AggregationVersion 2 makes DailyReportJob rebuild every day.
ALTER TABLE report.daily_histograms ADD COLUMN IF NOT EXISTS value_max DOUBLE PRECISION NOT NULL DEFAULT 0;
