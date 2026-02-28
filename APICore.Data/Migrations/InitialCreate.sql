-- Script generado desde 20260212154658_InitialCreate.cs
-- Ejecutar con: sqlcmd -S 127.0.0.1,1433 -U admin -P Mario010830 -d InventoryMain -C -i InitialCreate.sql
-- (con el túnel SSH abierto)

IF OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NOT NULL
    RETURN;
GO

CREATE TABLE [Locations] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(max) NULL,
    [Code] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Locations] PRIMARY KEY ([Id])
);

CREATE TABLE [Log] (
    [Id] int NOT NULL IDENTITY(1,1),
    [EventType] int NOT NULL,
    [LogType] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    [Description] nvarchar(max) NULL,
    [App] nvarchar(max) NULL,
    [Module] nvarchar(max) NULL,
    CONSTRAINT [PK_Log] PRIMARY KEY ([Id])
);

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Code] nvarchar(max) NULL,
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductCategories] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Color] nvarchar(max) NOT NULL,
    [Icon] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ProductCategories] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [IsSystem] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Setting] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Key] nvarchar(max) NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_Setting] PRIMARY KEY ([Id])
);

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Name] nvarchar(max) NULL,
    [ContactPerson] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY(1,1),
    [Code] nvarchar(max) NULL,
    [Name] nvarchar(max) NULL,
    [Description] nvarchar(max) NULL,
    [CategoryId] int NOT NULL,
    [Precio] decimal(18,2) NOT NULL,
    [Costo] decimal(18,2) NOT NULL,
    [ImagenUrl] nvarchar(max) NULL,
    [IsAvailable] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_ProductCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ProductCategories] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY(1,1),
    [BirthDate] datetime2 NOT NULL,
    [FullName] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Password] nvarchar(max) NULL,
    [GoogleId] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [LastLoggedIn] datetimeoffset NULL,
    [LocationId] int NULL,
    [RoleId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]),
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id])
);

CREATE TABLE [Inventories] (
    [Id] int NOT NULL IDENTITY(1,1),
    [ProductId] int NOT NULL,
    [LocationId] int NOT NULL,
    [CurrentStock] decimal(18,2) NOT NULL,
    [MinimumStock] decimal(18,2) NOT NULL,
    [UnitOfMeasure] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Inventories_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [InventoryMovements] (
    [Id] int NOT NULL IDENTITY(1,1),
    [ProductId] int NOT NULL,
    [LocationId] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [PreviousStock] decimal(18,2) NULL,
    [NewStock] decimal(18,2) NULL,
    [UnitCost] decimal(18,2) NULL,
    [UnitPrice] decimal(18,2) NULL,
    [Reason] nvarchar(max) NULL,
    [SupplierId] int NULL,
    [ReferenceDocument] nvarchar(max) NULL,
    [UserId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryMovements_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InventoryMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InventoryMovements_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
);

CREATE TABLE [UserToken] (
    [Id] int NOT NULL IDENTITY(1,1),
    [AccessToken] nvarchar(max) NULL,
    [AccessTokenExpiresDateTime] datetimeoffset NOT NULL,
    [RefreshToken] nvarchar(max) NULL,
    [RefreshTokenExpiresDateTime] datetimeoffset NOT NULL,
    [UserId] int NOT NULL,
    [DeviceBrand] nvarchar(max) NULL,
    [DeviceModel] nvarchar(max) NULL,
    [OS] nvarchar(max) NULL,
    [OSPlatform] nvarchar(max) NULL,
    [OSVersion] nvarchar(max) NULL,
    [ClientName] nvarchar(max) NULL,
    [ClientType] nvarchar(max) NULL,
    [ClientVersion] nvarchar(max) NULL,
    CONSTRAINT [PK_UserToken] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserToken_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Inventories_LocationId] ON [Inventories] ([LocationId]);
CREATE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);
CREATE INDEX [IX_InventoryMovements_LocationId] ON [InventoryMovements] ([LocationId]);
CREATE INDEX [IX_InventoryMovements_ProductId] ON [InventoryMovements] ([ProductId]);
CREATE INDEX [IX_InventoryMovements_SupplierId] ON [InventoryMovements] ([SupplierId]);
CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
CREATE INDEX [IX_Users_LocationId] ON [Users] ([LocationId]);
CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
CREATE INDEX [IX_UserToken_UserId] ON [UserToken] ([UserId]);

CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260212154658_InitialCreate', N'9.0.11');
GO
