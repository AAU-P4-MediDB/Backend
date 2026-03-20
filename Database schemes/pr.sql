CREATE TABLE PR (
                         uuid          UUID          PRIMARY KEY DEFAULT uuid_generate_v4(),
                         name          TEXT          NOT NULL CHECK (LENGTH(name) < 100),
                         birthdate     DATE          NOT NULL,
                         cpr_key       INTEGER       NOT NULL CHECK (cpr_key >= 0 AND cpr_key <= 9999),
                         bio_gender    BOOLEAN       NOT NULL,
                         pronouns      TEXT,
                         clinic        UUID          REFERENCES CCR(uuid) ON DELETE SET NULL,
                         doctor        UUID          REFERENCES CUR(uuid) ON DELETE SET NULL,
                         weight        DECIMAL       NOT NULL CHECK (weight > 0),
                         height        SMALLINT      NOT NULL CHECK (height > 0),
                         diagnosis     TEXT[],
                         vitals        JSON          NOT NULL,
                         prescriptions JSON,
                         pfp           TEXT,
                         journal       JSON          NOT NULL,
                         appointments  JSON,
                         lab_results   JSON
);