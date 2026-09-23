create database "DeveloperDB";

CREATE TABLE order_step1_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    order_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE route_stop_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    action TEXT NOT NULL,
    driver TEXT NOT NULL,
    office TEXT NOT NULL,
    event_id TEXT NOT NULL,
    vehicle_id TEXT NOT NULL,
    assignment_id TEXT NOT NULL,
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pickup_task_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    pickup_task_id TEXT NOT NULL,
    delivery_line_id TEXT NOT NULL DEFAULT '',
    event_id TEXT NOT NULL,
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE log_api_tokens (
    env TEXT PRIMARY KEY,
    access_token TEXT NOT NULL DEFAULT '',
    access_token_expired_at TIMESTAMPTZ NULL,
    refresh_token TEXT NOT NULL DEFAULT '',
    refresh_token_expired_at TIMESTAMPTZ NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_order_step1_trace_logs_log_id ON public.order_step1_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_order_step1_trace_logs_order_id ON public.order_step1_trace_logs (order_id, log_timestamp DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_route_stop_trace_logs_log_id ON public.route_stop_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_route_stop_trace_logs_vehicle_id ON public.route_stop_trace_logs (vehicle_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_route_stop_trace_logs_assignment_id ON public.route_stop_trace_logs (assignment_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_route_stop_trace_logs_log_timestamp ON public.route_stop_trace_logs (log_timestamp DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_pickup_task_trace_logs_log_id ON public.pickup_task_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pickup_task_trace_logs_pickup_task_id ON public.pickup_task_trace_logs (pickup_task_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pickup_task_trace_logs_delivery_line_id ON public.pickup_task_trace_logs (delivery_line_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pickup_task_trace_logs_log_timestamp ON public.pickup_task_trace_logs (log_timestamp DESC);

CREATE SCHEMA IF NOT EXISTS pro;

CREATE TABLE pro.order_step1_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    order_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pro.route_stop_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    action TEXT NOT NULL,
    driver TEXT NOT NULL,
    office TEXT NOT NULL,
    event_id TEXT NOT NULL,
    vehicle_id TEXT NOT NULL,
    assignment_id TEXT NOT NULL,
    action_object_id TEXT NOT NULL DEFAULT '',
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pro.pickup_task_trace_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    pickup_task_id TEXT NOT NULL,
    delivery_line_id TEXT NOT NULL DEFAULT '',
    event_id TEXT NOT NULL,
    message_detail TEXT NOT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);


CREATE TABLE pro.log_api_tokens (
    env TEXT PRIMARY KEY,
    access_token TEXT NOT NULL DEFAULT '',
    access_token_expired_at TIMESTAMPTZ NULL,
    refresh_token TEXT NOT NULL DEFAULT '',
    refresh_token_expired_at TIMESTAMPTZ NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_order_step1_trace_logs_log_id ON pro.order_step1_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_order_step1_trace_logs_order_id ON pro.order_step1_trace_logs (order_id, log_timestamp DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_route_stop_trace_logs_log_id ON pro.route_stop_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_route_stop_trace_logs_vehicle_id ON pro.route_stop_trace_logs (vehicle_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_route_stop_trace_logs_assignment_id ON pro.route_stop_trace_logs (assignment_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_route_stop_trace_logs_action_object_id ON pro.route_stop_trace_logs (action_object_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_route_stop_trace_logs_log_timestamp ON pro.route_stop_trace_logs (log_timestamp DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_pickup_task_trace_logs_log_id ON pro.pickup_task_trace_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_trace_logs_pickup_task_id ON pro.pickup_task_trace_logs (pickup_task_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_trace_logs_delivery_line_id ON pro.pickup_task_trace_logs (delivery_line_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_trace_logs_log_timestamp ON pro.pickup_task_trace_logs (log_timestamp DESC);

CREATE TABLE pro.pickup_task_orders (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    trace_id VARCHAR(64) NOT NULL DEFAULT '',
    pickup_task_id VARCHAR(50) NOT NULL,
    order_id VARCHAR(50) NOT NULL,
    extra_services VARCHAR(50) NOT NULL DEFAULT '',
    weight NUMERIC(18, 3) NOT NULL DEFAULT 0,
    real_weight NUMERIC(18, 3) NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'Khởi tạo',
    is_process_completed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pro.pickup_task_order_items (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    trace_id VARCHAR(64) NOT NULL DEFAULT '',
    pickup_task_id VARCHAR(50) NOT NULL,
    order_id VARCHAR(50) NOT NULL,
    order_item_id VARCHAR(50) NOT NULL,
    weight NUMERIC(18, 3) NOT NULL DEFAULT 0,
    real_weight NUMERIC(18, 3) NOT NULL DEFAULT 0,
    status VARCHAR(50) NOT NULL DEFAULT 'Khởi tạo',
    is_process_completed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_orders_pickup_task_id_order_id ON pro.pickup_task_orders (pickup_task_id, order_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_orders_order_id ON pro.pickup_task_orders (order_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_order_items_pickup_task_id_order_item_id ON pro.pickup_task_order_items (pickup_task_id, order_item_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_order_items_order_id ON pro.pickup_task_order_items (order_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_orders_trace_id ON pro.pickup_task_orders (trace_id);
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_order_items_trace_id ON pro.pickup_task_order_items (trace_id);

ALTER TABLE pro.pickup_task_trace_logs ADD COLUMN IF NOT EXISTS is_processed BOOLEAN NOT NULL DEFAULT FALSE;
CREATE INDEX IF NOT EXISTS idx_pro_pickup_task_trace_logs_unprocessed ON pro.pickup_task_trace_logs (log_timestamp, id) WHERE NOT is_processed;
