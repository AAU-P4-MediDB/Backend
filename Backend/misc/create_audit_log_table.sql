-- NFR-02: append-only audit log for patient journal view/edit access.
-- Run this against the application's Postgres database — this project has
-- no EF Core migrations, schema is applied by hand (see other tables' shape
-- in Models/db tables/*.cs for the pattern this follows).

CREATE TABLE IF NOT EXISTS audit_log (
    id           BIGSERIAL PRIMARY KEY,
    "timestamp"  TIMESTAMPTZ NOT NULL DEFAULT now(),
    user_uuid    UUID NOT NULL,
    patient_uuid UUID NOT NULL,
    action       TEXT NOT NULL,
    resource     TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_log_patient_uuid ON audit_log (patient_uuid);
CREATE INDEX IF NOT EXISTS idx_audit_log_user_uuid ON audit_log (user_uuid);
CREATE INDEX IF NOT EXISTS idx_audit_log_timestamp ON audit_log ("timestamp");

-- Append-only at the DB level: revoke UPDATE/DELETE from the application
-- role so a compromised app can still only insert, never tamper with or
-- erase existing entries. Replace medidb_app with the actual role the
-- backend connects as.
-- REVOKE UPDATE, DELETE ON audit_log FROM medidb_app;

-- NFR-02 requires 12-month retention, i.e. rows must NOT be deleted before
-- they are 12 months old — this table intentionally has no automatic
-- deletion. If/when old rows do need to be purged after the retention
-- window, that should be a separate, explicitly reviewed job, not an
-- automatic trigger, given this is meant to be tamper-evident audit
-- evidence for health data access.
