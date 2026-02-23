-- Migration: Initial Schema — ProviderOptimizerService
-- Version: 001
-- Description: Creates tables for providers, users and assistance requests

CREATE TABLE IF NOT EXISTS users (
    "Id"            UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Username"      VARCHAR(50)   NOT NULL,
    "Email"         VARCHAR(200)  NOT NULL,
    "PasswordHash"  TEXT          NOT NULL,
    "Role"          INT           NOT NULL DEFAULT 1,
    "CreatedAt"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_users_email UNIQUE ("Email")
);

CREATE TABLE IF NOT EXISTS providers (
    "Id"                UUID           NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "Name"              VARCHAR(100)   NOT NULL,
    "PhoneNumber"       VARCHAR(20),
    "Type"              INT            NOT NULL,
    "IsAvailable"       BOOLEAN        NOT NULL DEFAULT TRUE,
    "Rating"            NUMERIC(3,2)   NOT NULL DEFAULT 5.00,
    "ActiveAssignments" INT            NOT NULL DEFAULT 0,
    "TotalAssignments"  INT            NOT NULL DEFAULT 0,
    "latitude"          DOUBLE PRECISION NOT NULL,
    "longitude"         DOUBLE PRECISION NOT NULL,
    "CreatedAt"         TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "UpdatedAt"         TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS assistance_requests (
    "Id"                 UUID           NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "RequestorName"      VARCHAR(100)   NOT NULL,
    "RequiredType"       INT            NOT NULL,
    "Status"             INT            NOT NULL DEFAULT 1,
    "AssignedProviderId" UUID,
    "Notes"              VARCHAR(500),
    "latitude"           DOUBLE PRECISION NOT NULL,
    "longitude"          DOUBLE PRECISION NOT NULL,
    "CreatedAt"          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    "UpdatedAt"          TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_ar_provider FOREIGN KEY ("AssignedProviderId")
        REFERENCES providers ("Id") ON DELETE SET NULL
);

-- Indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_providers_available ON providers ("IsAvailable") WHERE "IsAvailable" = TRUE;
CREATE INDEX IF NOT EXISTS idx_providers_type      ON providers ("Type");
CREATE INDEX IF NOT EXISTS idx_ar_status           ON assistance_requests ("Status");
CREATE INDEX IF NOT EXISTS idx_ar_provider         ON assistance_requests ("AssignedProviderId");

-- Seed data — demonstration providers in Lima, Peru
INSERT INTO providers ("Id","Name","PhoneNumber","Type","IsAvailable","Rating","latitude","longitude","CreatedAt","UpdatedAt")
VALUES
    (gen_random_uuid(), 'Grúas Express Lima',    '+51 999 111 001', 1, TRUE, 4.80, -12.046374, -77.042793, NOW(), NOW()),
    (gen_random_uuid(), 'Cerrajeros 24h Miraflores', '+51 999 111 002', 2, TRUE, 4.60, -12.121700, -77.029600, NOW(), NOW()),
    (gen_random_uuid(), 'Baterías Rápido Peru', '+51 999 111 003', 3, TRUE, 4.90, -12.063400, -77.036600, NOW(), NOW()),
    (gen_random_uuid(), 'Grúas Norte Lima',      '+51 999 111 004', 1, TRUE, 4.30, -11.990000, -77.010000, NOW(), NOW()),
    (gen_random_uuid(), 'Neumáticos SOS',        '+51 999 111 005', 4, FALSE, 4.70, -12.080000, -77.050000, NOW(), NOW())
ON CONFLICT DO NOTHING;
