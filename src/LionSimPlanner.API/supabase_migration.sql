DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'maint') THEN
        CREATE SCHEMA maint;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS maint.__efmigrations (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___efmigrations PRIMARY KEY (migration_id)
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'maint') THEN
        CREATE SCHEMA maint;
    END IF;
END $EF$;

CREATE TABLE maint.engineers (
    engineer_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    employee_code character varying(50) NOT NULL,
    full_name character varying(200) NOT NULL,
    clearance_level character varying(50) NOT NULL,
    hardware_ratings jsonb NOT NULL,
    shift_start_time timestamp with time zone NOT NULL,
    shift_end_time timestamp with time zone NOT NULL,
    is_on_call boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_engineers" PRIMARY KEY (engineer_id)
);

CREATE TABLE maint.maintenance_checklists (
    checklist_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    simulator_id uuid NOT NULL,
    engineer_id_ref uuid NOT NULL,
    engineer_code character varying(50) NOT NULL,
    checklist_date date NOT NULL,
    is_cleared boolean NOT NULL DEFAULT FALSE,
    notes text NOT NULL,
    signed_off_at timestamp with time zone,
    blocking_reason text,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_maintenance_checklists" PRIMARY KEY (checklist_id)
);

CREATE TABLE maint.simulators (
    simulator_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    name character varying(100) NOT NULL,
    bay_number character varying(20) NOT NULL,
    aircraft_type character varying(50) NOT NULL,
    status character varying(30) NOT NULL DEFAULT 'Ready',
    last_status_changed_by_engineer_id uuid,
    last_status_changed_by_engineer_code character varying(50),
    last_status_changed_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_simulators" PRIMARY KEY (simulator_id)
);

CREATE UNIQUE INDEX "IX_engineers_employee_code" ON maint.engineers (employee_code);

CREATE UNIQUE INDEX uq_checklist_simulator_date ON maint.maintenance_checklists (simulator_id, checklist_date);

INSERT INTO maint.__efmigrations (migration_id, product_version)
VALUES ('20260714102801_InitialCreate', '9.0.4');

ALTER TABLE maint.engineers ADD checkout_time timestamp with time zone;

CREATE TABLE maint.maintenance_logs (
    maintenance_log_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    simulator_id uuid NOT NULL,
    severity character varying(30) NOT NULL,
    fault_description text NOT NULL,
    resolution_description text,
    resolved_at timestamp with time zone,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_maintenance_logs" PRIMARY KEY (maintenance_log_id),
    CONSTRAINT "FK_maintenance_logs_simulators_simulator_id" FOREIGN KEY (simulator_id) REFERENCES maint.simulators (simulator_id) ON DELETE CASCADE
);

CREATE INDEX idx_maintenance_logs_resolved_at ON maint.maintenance_logs (resolved_at);

CREATE INDEX idx_maintenance_logs_simulator_id ON maint.maintenance_logs (simulator_id);

INSERT INTO maint.__efmigrations (migration_id, product_version)
VALUES ('20260719164844_AddMaintenanceLifecycleEntities', '9.0.4');

ALTER TABLE maint.maintenance_logs DROP CONSTRAINT "FK_maintenance_logs_simulators_simulator_id";

ALTER TABLE maint.simulators DROP CONSTRAINT "PK_simulators";

ALTER TABLE maint.maintenance_logs DROP CONSTRAINT "PK_maintenance_logs";

ALTER TABLE maint.maintenance_checklists DROP CONSTRAINT "PK_maintenance_checklists";

ALTER TABLE maint.engineers DROP CONSTRAINT "PK_engineers";

ALTER INDEX maint."IX_engineers_employee_code" RENAME TO ix_engineers_employee_code;

ALTER TABLE maint.simulators ADD CONSTRAINT pk_simulators PRIMARY KEY (simulator_id);

ALTER TABLE maint.maintenance_logs ADD CONSTRAINT pk_maintenance_logs PRIMARY KEY (maintenance_log_id);

ALTER TABLE maint.maintenance_checklists ADD CONSTRAINT pk_maintenance_checklists PRIMARY KEY (checklist_id);

ALTER TABLE maint.engineers ADD CONSTRAINT pk_engineers PRIMARY KEY (engineer_id);

ALTER TABLE maint.maintenance_logs ADD CONSTRAINT fk_maintenance_logs_simulators_simulator_id FOREIGN KEY (simulator_id) REFERENCES maint.simulators (simulator_id) ON DELETE CASCADE;

INSERT INTO maint.__efmigrations (migration_id, product_version)
VALUES ('20260721091146_SyncModelSnapshot', '9.0.4');

CREATE TABLE maint.simulator_defects (
    defect_id uuid NOT NULL DEFAULT (gen_random_uuid()),
    simulator_id uuid NOT NULL,
    session_id uuid,
    reported_by character varying(200) NOT NULL,
    system_affected character varying(100) NOT NULL,
    severity character varying(30) NOT NULL,
    instructor_notes text NOT NULL,
    status character varying(30) NOT NULL DEFAULT 'Open',
    resolution_notes text,
    resolved_by_engineer_id uuid,
    resolved_by_engineer_code character varying(50),
    resolved_at timestamp with time zone,
    reported_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_simulator_defects PRIMARY KEY (defect_id),
    CONSTRAINT fk_simulator_defects_simulators_simulator_id FOREIGN KEY (simulator_id) REFERENCES maint.simulators (simulator_id) ON DELETE CASCADE
);

CREATE INDEX idx_defects_severity ON maint.simulator_defects (severity);

CREATE INDEX idx_defects_simulator_id ON maint.simulator_defects (simulator_id);

CREATE INDEX idx_defects_status ON maint.simulator_defects (status);

INSERT INTO maint.__efmigrations (migration_id, product_version)
VALUES ('20260727082011_AddSimulatorDefectsTable', '9.0.4');

COMMIT;

