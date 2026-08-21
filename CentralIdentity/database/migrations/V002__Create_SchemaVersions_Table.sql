-- ============================================================
-- Script: V002__Create_SchemaVersions_Table.sql
-- Description: Creates a schema version tracking table so
--              database migrations can be applied idempotently.
-- ============================================================

USE CentralIdentityDb;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[SchemaVersions]')
      AND type = 'U'
)
BEGIN
    CREATE TABLE [dbo].[SchemaVersions] (
        [Id]            INT            NOT NULL IDENTITY(1,1),
        [ScriptName]    NVARCHAR(255)  NOT NULL,
        [AppliedOn]     DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
        [Checksum]      NVARCHAR(100)  NULL,
        CONSTRAINT [PK_SchemaVersions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO
