CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE CCR (
    uuid      UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    name      TEXT         NOT NULL CHECK (LENGTH(name) < 1000),
    location  TEXT         NOT NULL CHECK (LENGTH(location) < 1000),
    email     TEXT         CHECK (LENGTH(email) < 100) UNIQUE ,
    cvr       INTEGER      NOT NULL CHECK (cvr >= 0 AND cvr <= 99999999),
    phone     INTEGER      NOT NULL CHECK (phone >= 0 AND phone <= 99999999)
);