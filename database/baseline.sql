-- One-time transition for a database created by EnsureCreatedAsync.
-- Run ONLY after comparing its full schema with InitialCreate and taking a backup.
-- Empty databases must use MigrateAsync directly, without this baseline.
BEGIN;
CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261016135752_InitialCreate', '9.0.20')
ON CONFLICT ("MigrationId") DO NOTHING;
COMMIT;
