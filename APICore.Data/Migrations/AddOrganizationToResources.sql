-- ============================================================
-- Script: Añadir OrganizationId a Product, ProductCategory,
--         Supplier, Role, Setting y Log (filtro por organización)
-- Ejecutar en la base de datos SQL Server (DBeaver, SSMS, etc.).
-- Asegúrate de que exista al menos una fila en Organizations
-- antes de ejecutar (para el backfill de Products/Categories/Suppliers).
-- Usa EXEC para evitar error de columna no existente en el mismo batch.
-- ============================================================

BEGIN TRANSACTION;

-- ----- Products -----
ALTER TABLE [Products] ADD [OrganizationId] INT NULL;

EXEC('UPDATE [Products] SET [OrganizationId] = (SELECT TOP 1 [Id] FROM [Organizations])');

ALTER TABLE [Products] ALTER COLUMN [OrganizationId] INT NOT NULL;

CREATE INDEX [IX_Products_OrganizationId] ON [Products] ([OrganizationId]);

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;

-- ----- ProductCategories -----
ALTER TABLE [ProductCategories] ADD [OrganizationId] INT NULL;

EXEC('UPDATE [ProductCategories] SET [OrganizationId] = (SELECT TOP 1 [Id] FROM [Organizations])');

ALTER TABLE [ProductCategories] ALTER COLUMN [OrganizationId] INT NOT NULL;

CREATE INDEX [IX_ProductCategories_OrganizationId] ON [ProductCategories] ([OrganizationId]);

ALTER TABLE [ProductCategories] ADD CONSTRAINT [FK_ProductCategories_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;

-- ----- Suppliers -----
ALTER TABLE [Suppliers] ADD [OrganizationId] INT NULL;

EXEC('UPDATE [Suppliers] SET [OrganizationId] = (SELECT TOP 1 [Id] FROM [Organizations])');

ALTER TABLE [Suppliers] ALTER COLUMN [OrganizationId] INT NOT NULL;

CREATE INDEX [IX_Suppliers_OrganizationId] ON [Suppliers] ([OrganizationId]);

ALTER TABLE [Suppliers] ADD CONSTRAINT [FK_Suppliers_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;

-- ----- Roles (OrganizationId nullable: roles de sistema = NULL) -----
ALTER TABLE [Roles] ADD [OrganizationId] INT NULL;

CREATE INDEX [IX_Roles_OrganizationId] ON [Roles] ([OrganizationId]);

ALTER TABLE [Roles] ADD CONSTRAINT [FK_Roles_Organizations_OrganizationId]
    FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION;

-- ----- Setting (OrganizationId nullable: configuración global = NULL) -----
ALTER TABLE [Setting] ADD [OrganizationId] INT NULL;

CREATE INDEX [IX_Setting_OrganizationId] ON [Setting] ([OrganizationId]);

-- ----- Log (OrganizationId nullable) -----
ALTER TABLE [Log] ADD [OrganizationId] INT NULL;

CREATE INDEX [IX_Log_OrganizationId] ON [Log] ([OrganizationId]);

COMMIT TRANSACTION;
