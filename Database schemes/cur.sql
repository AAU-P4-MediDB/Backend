CREATE TYPE position_type AS ENUM (
    secretary,
    Nurse,
    Doctor,
    Local_administrator,
    System_administrator,);

CREATE TABLE CUR (
                        uuid      UUID           PRIMARY KEY DEFAULT uuid_generate_v4(),
                        email     TEXT           NOT NULL CHECK (LENGTH(email) < 200),
                        password  TEXT           NOT NULL,
                        salt      TEXT           NOT NULL,
                        name      TEXT           NOT NULL CHECK (LENGTH(name) < 1000),
                        position  position_type  NOT NULL,
                        pfp       TEXT,
                        clinic    UUID           REFERENCES CCR(uuid) ON DELETE SET NULL,
                        phone     INTEGER        NOT NULL CHECK (phone >= 0 AND phone <= 99999999)
);