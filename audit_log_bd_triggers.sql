-- Audit log en BD (PostgreSQL) + created_at/updated_at en tablas de RBAC
-- Alcance (por ahora): roles, permissions, role_permissions
-- Escritura: triggers I/U/D que guardan OldData/NewData (jsonb) en audit_logs.
--
-- Importante:
-- - app.user_id es opcional. Si no existe, audit_log.AppUserId queda NULL.
-- - No se guarda IP (por decisión del sprint/prototipo).

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- =========================
-- audit_logs (tabla central)
-- =========================
CREATE TABLE IF NOT EXISTS audit_logs (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "OccurredAt" timestamptz NOT NULL DEFAULT now(),
    "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT now(),

    "TableName" text NOT NULL,
    "Operation" char(1) NOT NULL CHECK ("Operation" IN ('I','U','D')),
    "RowPk" text NOT NULL,

    "OldData" jsonb NULL,
    "NewData" jsonb NULL,

    "DbUser" text NOT NULL DEFAULT current_user,
    "AppUserId" text NULL
);

CREATE INDEX IF NOT EXISTS "IX_audit_logs_OccurredAt" ON audit_logs ("OccurredAt");
CREATE INDEX IF NOT EXISTS "IX_audit_logs_TableName" ON audit_logs ("TableName");
CREATE INDEX IF NOT EXISTS "IX_audit_logs_RowPk" ON audit_logs ("RowPk");

-- =========================
-- updated_at genérico
-- =========================
CREATE OR REPLACE FUNCTION set_updated_at_generic()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW."UpdatedAt" = now();
    RETURN NEW;
END;
$$;

-- =====================================================
-- app.user_id opcional (dejar NULL si no está seteado)
-- =====================================================
CREATE OR REPLACE FUNCTION audit_get_app_user_id()
RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN NULLIF(current_setting('app.user_id', true), '');
END;
$$;

-- =========================================================
-- Roles: created_at/updated_at + audit de cambios (Name)
-- =========================================================
ALTER TABLE roles
    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamptz NOT NULL DEFAULT now();

DROP TRIGGER IF EXISTS trg_roles_set_updated_at ON roles;
CREATE TRIGGER trg_roles_set_updated_at
BEFORE UPDATE ON roles
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_generic();

CREATE OR REPLACE FUNCTION audit_roles_iud()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    old_json jsonb;
    new_json jsonb;
BEGIN
    IF (TG_OP = 'INSERT') THEN
        new_json := jsonb_build_object(
            'Id', NEW."Id",
            'Name', NEW."Name"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('roles','I', NEW."Id"::text, NULL, new_json, audit_get_app_user_id());

        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        -- Solo auditar si cambió el campo de negocio
        IF NEW."Name" IS DISTINCT FROM OLD."Name" THEN
            old_json := jsonb_build_object('Name', OLD."Name");
            new_json := jsonb_build_object('Name', NEW."Name");

            INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
            VALUES ('roles','U', NEW."Id"::text, old_json, new_json, audit_get_app_user_id());
        END IF;

        RETURN NEW;
    ELSE -- DELETE
        old_json := jsonb_build_object(
            'Id', OLD."Id",
            'Name', OLD."Name"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('roles','D', OLD."Id"::text, old_json, NULL, audit_get_app_user_id());

        RETURN OLD;
    END IF;
END;
$$;

DROP TRIGGER IF EXISTS trg_roles_audit_iud ON roles;
CREATE TRIGGER trg_roles_audit_iud
AFTER INSERT OR UPDATE OR DELETE ON roles
FOR EACH ROW
EXECUTE FUNCTION audit_roles_iud();

-- =========================================================
-- Permissions: created_at/updated_at + audit de cambios
-- =========================================================
ALTER TABLE permissions
    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamptz NOT NULL DEFAULT now();

DROP TRIGGER IF EXISTS trg_permissions_set_updated_at ON permissions;
CREATE TRIGGER trg_permissions_set_updated_at
BEFORE UPDATE ON permissions
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_generic();

CREATE OR REPLACE FUNCTION audit_permissions_iud()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    old_json jsonb;
    new_json jsonb;
BEGIN
    IF (TG_OP = 'INSERT') THEN
        new_json := jsonb_build_object(
            'Id', NEW."Id",
            'Name', NEW."Name",
            'Description', NEW."Description",
            'Resource', NEW."Resource",
            'Action', NEW."Action"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('permissions','I', NEW."Id"::text, NULL, new_json, audit_get_app_user_id());

        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        old_json := '{}'::jsonb;
        new_json := '{}'::jsonb;

        IF NEW."Name" IS DISTINCT FROM OLD."Name" THEN
            old_json := old_json || jsonb_build_object('Name', OLD."Name");
            new_json := new_json || jsonb_build_object('Name', NEW."Name");
        END IF;

        IF NEW."Description" IS DISTINCT FROM OLD."Description" THEN
            old_json := old_json || jsonb_build_object('Description', OLD."Description");
            new_json := new_json || jsonb_build_object('Description', NEW."Description");
        END IF;

        IF NEW."Resource" IS DISTINCT FROM OLD."Resource" THEN
            old_json := old_json || jsonb_build_object('Resource', OLD."Resource");
            new_json := new_json || jsonb_build_object('Resource', NEW."Resource");
        END IF;

        IF NEW."Action" IS DISTINCT FROM OLD."Action" THEN
            old_json := old_json || jsonb_build_object('Action', OLD."Action");
            new_json := new_json || jsonb_build_object('Action', NEW."Action");
        END IF;

        -- Solo auditar si al menos una columna cambió
        IF new_json <> '{}'::jsonb THEN
            INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
            VALUES ('permissions','U', NEW."Id"::text, old_json, new_json, audit_get_app_user_id());
        END IF;

        RETURN NEW;
    ELSE -- DELETE
        old_json := jsonb_build_object(
            'Id', OLD."Id",
            'Name', OLD."Name",
            'Description', OLD."Description",
            'Resource', OLD."Resource",
            'Action', OLD."Action"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('permissions','D', OLD."Id"::text, old_json, NULL, audit_get_app_user_id());

        RETURN OLD;
    END IF;
END;
$$;

DROP TRIGGER IF EXISTS trg_permissions_audit_iud ON permissions;
CREATE TRIGGER trg_permissions_audit_iud
AFTER INSERT OR UPDATE OR DELETE ON permissions
FOR EACH ROW
EXECUTE FUNCTION audit_permissions_iud();

-- =========================================================
-- role_permissions (puente): created_at/updated_at + audit
-- =========================================================
ALTER TABLE role_permissions
    ADD COLUMN IF NOT EXISTS "CreatedAt" timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "UpdatedAt" timestamptz NOT NULL DEFAULT now();

DROP TRIGGER IF EXISTS trg_role_permissions_set_updated_at ON role_permissions;
CREATE TRIGGER trg_role_permissions_set_updated_at
BEFORE UPDATE ON role_permissions
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_generic();

CREATE OR REPLACE FUNCTION audit_role_permissions_iud()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    old_json jsonb;
    new_json jsonb;
    pk text;
BEGIN
    IF (TG_OP = 'INSERT') THEN
        pk := format('RoleId=%s;PermissionId=%s', NEW."RoleId"::text, NEW."PermissionId"::text);

        new_json := jsonb_build_object(
            'RoleId', NEW."RoleId",
            'PermissionId', NEW."PermissionId"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('role_permissions','I', pk, NULL, new_json, audit_get_app_user_id());

        RETURN NEW;
    ELSIF (TG_OP = 'UPDATE') THEN
        pk := format('RoleId=%s;PermissionId=%s', NEW."RoleId"::text, NEW."PermissionId"::text);

        IF NEW."RoleId" IS DISTINCT FROM OLD."RoleId" OR NEW."PermissionId" IS DISTINCT FROM OLD."PermissionId" THEN
            old_json := jsonb_build_object(
                'RoleId', OLD."RoleId",
                'PermissionId', OLD."PermissionId"
            );
            new_json := jsonb_build_object(
                'RoleId', NEW."RoleId",
                'PermissionId', NEW."PermissionId"
            );

            INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
            VALUES ('role_permissions','U', pk, old_json, new_json, audit_get_app_user_id());
        END IF;

        RETURN NEW;
    ELSE -- DELETE
        pk := format('RoleId=%s;PermissionId=%s', OLD."RoleId"::text, OLD."PermissionId"::text);

        old_json := jsonb_build_object(
            'RoleId', OLD."RoleId",
            'PermissionId', OLD."PermissionId"
        );

        INSERT INTO audit_logs("TableName","Operation","RowPk","OldData","NewData","AppUserId")
        VALUES ('role_permissions','D', pk, old_json, NULL, audit_get_app_user_id());

        RETURN OLD;
    END IF;
END;
$$;

DROP TRIGGER IF EXISTS trg_role_permissions_audit_iud ON role_permissions;
CREATE TRIGGER trg_role_permissions_audit_iud
AFTER INSERT OR UPDATE OR DELETE ON role_permissions
FOR EACH ROW
EXECUTE FUNCTION audit_role_permissions_iud();

-- audit_logs updated_at (opcional; por ahora no se actualiza)
DROP TRIGGER IF EXISTS trg_audit_logs_set_updated_at ON audit_logs;
CREATE TRIGGER trg_audit_logs_set_updated_at
BEFORE UPDATE ON audit_logs
FOR EACH ROW
EXECUTE FUNCTION set_updated_at_generic();

COMMIT;

