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

CREATE TABLE drivers (
    driver_id UUID PRIMARY KEY,
    "name" VARCHAR(255) NOT NULL,
    bearer_token TEXT NOT NULL,
    token_expired_at TIMESTAMPTZ NULL,
    email VARCHAR(255) NOT NULL,
    code VARCHAR(50) NOT NULL
);

CREATE INDEX idx_route_checkpoints_template_id ON route_checkpoints (template_id);

-- init data for vehicles table
INSERT INTO
    vehicles (license_plate, last_odo, is_moving)
VALUES
    ('50h22177', 0, false),
    ('50h07276', 0, false),
    ('51c87400', 0, false),
    ('50h22620', 0, false),
    ('50h22600', 0, false),
    ('29c61163', 0, false),
    ('50h08069', 0, false),
    ('29c61606', 0, false),
    ('50h10606', 0, false),
    ('51d64584', 0, false),
    ('51c39685', 0, false),
    ('50h21055', 0, false),
    ('50h22378', 0, false),
    ('50h24670', 0, false),
    ('50h00441', 0, false),
    ('50g01410', 0, false),
    ('50h22662', 0, false),
    ('51d64937', 0, false),
    ('29d30872', 0, false),
    ('30g35129', 0, false),
    ('51b50370', 0, false),
    ('51c60161', 0, false),
    ('51d43029', 0, false),
    ('50h20904', 0, false),
    ('50h20901', 0, false),
    ('51b50271', 0, false),
    ('50g02257', 0, false),
    ('51b29873', 0, false),
    ('51b29785', 0, false),
    ('51b50371', 0, false);

-- init data for drivers table
INSERT INTO
    drivers (
        driver_id,
        code,
        "name",
        bearer_token,
        token_expired_at,
        email
    )
VALUES
    (
        '6a3994cf-1696-4d6f-ab45-10bde5f3dec9',
        'EMP_00202',
        'Phạm Phan Minh Đức',
        '',
        NULL,
        'duc.pham.phan.minh@example.com'
    );