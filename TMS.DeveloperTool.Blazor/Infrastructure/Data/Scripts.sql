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

CREATE TABLE IF NOT EXISTS pro.delivery_manifest_commit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    delivery_manifest_code TEXT NOT NULL DEFAULT '',
    cod_manifest_code TEXT NOT NULL DEFAULT '',
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_manifest_commit_logs_log_id ON pro.delivery_manifest_commit_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_manifest_commit_logs_delivery_manifest_code ON pro.delivery_manifest_commit_logs (delivery_manifest_code);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_manifest_commit_logs_cod_manifest_code ON pro.delivery_manifest_commit_logs (cod_manifest_code);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_manifest_commit_logs_actor ON pro.delivery_manifest_commit_logs (actor, log_timestamp DESC);

CREATE TABLE IF NOT EXISTS pro.delivery_task_complete_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    delivery_manifest_code TEXT NOT NULL DEFAULT '',
    task_count INT NOT NULL DEFAULT 0,
    manifest_count INT NOT NULL DEFAULT 0,
    delivered_count INT NOT NULL DEFAULT 0,
    collected_cod NUMERIC(18, 2) NOT NULL DEFAULT 0,
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_task_complete_logs_log_id ON pro.delivery_task_complete_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_task_complete_logs_delivery_manifest_code ON pro.delivery_task_complete_logs (delivery_manifest_code, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_task_complete_logs_log_timestamp ON pro.delivery_task_complete_logs (log_timestamp DESC);

CREATE TABLE IF NOT EXISTS pro.delivery_failure_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    delivery_manifest_code TEXT NOT NULL DEFAULT '',
    record_id TEXT NOT NULL DEFAULT '',
    failure_type TEXT NOT NULL DEFAULT '',
    task_count INT NOT NULL DEFAULT 0,
    manifest_count INT NOT NULL DEFAULT 0,
    item_count INT NOT NULL DEFAULT 0,
    driver_id TEXT NOT NULL DEFAULT '',
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_failure_logs_log_id ON pro.delivery_failure_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_failure_logs_failure_type ON pro.delivery_failure_logs (failure_type, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_failure_logs_driver_id ON pro.delivery_failure_logs (driver_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_failure_logs_delivery_manifest_code ON pro.delivery_failure_logs (delivery_manifest_code);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_failure_logs_log_timestamp ON pro.delivery_failure_logs (log_timestamp DESC);

CREATE TABLE IF NOT EXISTS pro.delivery_transfer_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    transfer_code TEXT NOT NULL DEFAULT '',
    source_type TEXT NOT NULL DEFAULT '',          -- 'Driver' (tài xế → tài xế) | 'Employee' (nhân viên bưu cục → tài xế)
    source_code TEXT NOT NULL DEFAULT '',
    target_driver_code TEXT NOT NULL DEFAULT '',
    order_ids TEXT[] NOT NULL DEFAULT '{}',
    order_count INT NOT NULL DEFAULT 0,
    transferred_at TIMESTAMPTZ NULL,
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_transfer_logs_log_id ON pro.delivery_transfer_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_transfer_logs_source ON pro.delivery_transfer_logs (source_type, source_code, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_transfer_logs_target_driver_code ON pro.delivery_transfer_logs (target_driver_code, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_transfer_logs_transfer_code ON pro.delivery_transfer_logs (transfer_code);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_transfer_logs_order_ids ON pro.delivery_transfer_logs USING GIN (order_ids);

CREATE TABLE IF NOT EXISTS pro.delivery_arrival_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    delivery_manifest_code TEXT NOT NULL DEFAULT '',
    arrival_id TEXT NOT NULL DEFAULT '',
    task_id TEXT NOT NULL DEFAULT '',
    order_id TEXT NOT NULL DEFAULT '',
    vehicle_id TEXT NOT NULL DEFAULT '',
    distance_meters NUMERIC(12, 2) NULL,
    is_gps_valid BOOLEAN NULL,
    superseded INT NOT NULL DEFAULT 0,
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_arrival_logs_log_id ON pro.delivery_arrival_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_arrival_logs_log_timestamp ON pro.delivery_arrival_logs (log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_arrival_logs_order_id ON pro.delivery_arrival_logs (order_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_arrival_logs_task_id ON pro.delivery_arrival_logs (task_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_arrival_logs_vehicle_id ON pro.delivery_arrival_logs (vehicle_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_arrival_logs_actor ON pro.delivery_arrival_logs (actor, log_timestamp DESC);

CREATE TABLE IF NOT EXISTS pro.delivery_session_commit_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    manifest_count INT NOT NULL DEFAULT 0,
    manifest_codes TEXT[] NOT NULL DEFAULT '{}',
    item_count INT NOT NULL DEFAULT 0,
    actor TEXT NOT NULL DEFAULT '',
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_delivery_session_commit_logs_log_id ON pro.delivery_session_commit_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_session_commit_logs_log_timestamp ON pro.delivery_session_commit_logs (log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_session_commit_logs_actor ON pro.delivery_session_commit_logs (actor, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_delivery_session_commit_logs_manifest_codes ON pro.delivery_session_commit_logs USING GIN (manifest_codes);

CREATE TABLE IF NOT EXISTS pro.unloading_handover_logs (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    log_id TEXT NOT NULL,
    trace_id TEXT NOT NULL,
    span_id TEXT NOT NULL,
    log_timestamp TIMESTAMPTZ NOT NULL,
    handover_code TEXT NOT NULL DEFAULT '',
    handover_id TEXT NOT NULL DEFAULT '',
    driver_id TEXT NOT NULL DEFAULT '',
    item_count INT NOT NULL DEFAULT 0,
    actor TEXT NOT NULL DEFAULT '',
    is_confirm BOOLEAN NOT NULL DEFAULT FALSE,
    confirm_at TIMESTAMPTZ NULL,
    confirm_trace_id TEXT NULL,
    is_received BOOLEAN NOT NULL DEFAULT FALSE,
    received_at TIMESTAMPTZ NULL,
    received_trace_id TEXT NULL,
    env TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_pro_unloading_handover_logs_log_id ON pro.unloading_handover_logs (log_id);
CREATE INDEX IF NOT EXISTS idx_pro_unloading_handover_logs_handover_code ON pro.unloading_handover_logs (handover_code);
CREATE INDEX IF NOT EXISTS idx_pro_unloading_handover_logs_driver_id ON pro.unloading_handover_logs (driver_id, log_timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_pro_unloading_handover_logs_log_timestamp ON pro.unloading_handover_logs (log_timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_pro_unloading_handover_logs_not_received ON pro.unloading_handover_logs (log_timestamp) WHERE NOT is_received;

CREATE INDEX IF NOT EXISTS idx_pro_unloading_handover_logs_not_confirm ON pro.unloading_handover_logs (log_timestamp) WHERE NOT is_confirm;
