CREATE TABLE route_checkpoint_templates (
    id UUID PRIMARY KEY,
    "name" VARCHAR(255) NOT NULL,
    jump_seconds INTEGER NOT NULL
);

CREATE TABLE route_checkpoints (
    id UUID PRIMARY KEY,
    lon float8 NOT NULL,
    lat float8 NOT NULL,
    "address" TEXT NOT NULL,
    km INTEGER NOT NULL,
    "order" INTEGER NOT NULL,
    template_id UUID NOT NULL,
    CONSTRAINT fk_route_checkpoints_template FOREIGN KEY (template_id) REFERENCES route_checkpoint_templates (id) ON DELETE CASCADE
);

CREATE TABLE vehicles (
    license_plate VARCHAR(8) PRIMARY KEY,
    last_odo float8 NOT NULL,
    is_moving BOOL NOT NULL
);

CREATE INDEX idx_route_checkpoints_template_id ON route_checkpoints (template_id);