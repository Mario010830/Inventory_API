-- ============================================================
-- Script combinado: AddOrganizationEntity + AddUserOrganizationAndSuperAdminRole
-- Ejecutar TODO (Ctrl+A) y "Execute SQL Script" en DBeaver.
-- ============================================================

BEGIN TRANSACTION;

-- 1) Crear tabla Organizations
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Organizations')
BEGIN
    CREATE TABLE [dbo].[Organizations] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Name] nvarchar(max) NULL,
        [Code] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id])
    );
END;

-- 2) Insertar organización por defecto
IF NOT EXISTS (SELECT 1 FROM [dbo].[Organizations] WHERE [Code] = N'DEFAULT')
BEGIN
    INSERT INTO [dbo].[Organizations] (Name, Code, Description, CreatedAt, ModifiedAt)
    VALUES (N'Organización Principal', N'DEFAULT', N'Organización por defecto', GETUTCDATE(), GETUTCDATE());
END;

-- 3) Añadir OrganizationId a Locations (ALTER directo; si ya existe, comentar esta línea)
ALTER TABLE [dbo].[Locations] ADD [OrganizationId] int NULL;
GO

-- 4) Asignar localizaciones a la organización por defecto
UPDATE [dbo].[Locations]
SET [OrganizationId] = (SELECT TOP 1 [Id] FROM [dbo].[Organizations] WHERE [Code] = N'DEFAULT')
WHERE [OrganizationId] IS NULL;

-- 5) Hacer OrganizationId NOT NULL en Locations
ALTER TABLE [dbo].[Locations] ALTER COLUMN [OrganizationId] int NOT NULL;

-- 6) Índice y FK en Locations
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Locations_OrganizationId' AND object_id = OBJECT_ID('dbo.Locations'))
    CREATE INDEX [IX_Locations_OrganizationId] ON [dbo].[Locations] ([OrganizationId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Locations_Organizations_OrganizationId')
    ALTER TABLE [dbo].[Locations]
    ADD CONSTRAINT [FK_Locations_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id]) ON DELETE CASCADE;

-- 7) Añadir OrganizationId a Users (si ya existe, comentar esta línea)
ALTER TABLE [dbo].[Users] ADD [OrganizationId] int NULL;

-- 8) Índice en Users
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_OrganizationId' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX [IX_Users_OrganizationId] ON [dbo].[Users] ([OrganizationId]);

-- 9) FK Users -> Organizations
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Organizations_OrganizationId')
    ALTER TABLE [dbo].[Users]
    ADD CONSTRAINT [FK_Users_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organizations] ([Id]);

COMMIT TRANSACTION;
