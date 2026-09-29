IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(200) NULL,
    [Barcode] nvarchar(100) NULL,
    [Cost] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629183919_InitialCreate', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260629203435_Initia', N'9.0.0');

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Description');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var0 + '];');
UPDATE [Products] SET [Description] = N'' WHERE [Description] IS NULL;
ALTER TABLE [Products] ALTER COLUMN [Description] nvarchar(200) NOT NULL;
ALTER TABLE [Products] ADD DEFAULT N'' FOR [Description];

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Barcode');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var1 + '];');
UPDATE [Products] SET [Barcode] = N'' WHERE [Barcode] IS NULL;
ALTER TABLE [Products] ALTER COLUMN [Barcode] nvarchar(100) NOT NULL;
ALTER TABLE [Products] ADD DEFAULT N'' FOR [Barcode];

ALTER TABLE [Products] ADD [Active] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [CategoryId] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [CreateBy] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Products] ADD [CreationDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Products] ADD [ExpirationDate] datetime2 NULL;

ALTER TABLE [Products] ADD [InvoiceWithoutStock] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Products] ADD [MaximumQuantity] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [MinimumQuantity] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [ModificationDate] datetime2 NULL;

ALTER TABLE [Products] ADD [ModifiedBy] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Products] ADD [ProductTypeId] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [Reference] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Products] ADD [ShortDescription] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [Products] ADD [UnitOfMeasurementId] int NOT NULL DEFAULT 0;

CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(100) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductTypes] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(100) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ProductTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [Identification] nvarchar(20) NOT NULL,
    [Gender] nvarchar(1) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [BirthDate] date NOT NULL,
    [UserName] nvarchar(50) NOT NULL,
    [CreationDate] datetime2 NOT NULL DEFAULT (GETDATE()),
    [Active] bit NOT NULL DEFAULT CAST(1 AS bit),
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Products_Barcode] ON [Products] ([Barcode]);

CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);

CREATE INDEX [IX_Products_ProductTypeId] ON [Products] ([ProductTypeId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_Identification] ON [Users] ([Identification]);

CREATE UNIQUE INDEX [IX_Users_UserName] ON [Users] ([UserName]);

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_ProductTypes_ProductTypeId] FOREIGN KEY ([ProductTypeId]) REFERENCES [ProductTypes] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260701181333_CompleteEntitiesSetup', N'9.0.0');

ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_Categories_CategoryId];

ALTER TABLE [Products] DROP CONSTRAINT [FK_Products_ProductTypes_ProductTypeId];

DROP INDEX [IX_Products_CategoryId] ON [Products];

DROP INDEX [IX_Products_ProductTypeId] ON [Products];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260701181716_RemoveProductForeignKeys', N'9.0.0');

DROP TABLE [ProductTypes];

DROP INDEX [IX_Users_Email] ON [Users];

DROP INDEX [IX_Users_Identification] ON [Users];

DROP INDEX [IX_Users_UserName] ON [Users];

DROP INDEX [IX_Products_Barcode] ON [Products];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UserName');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Users] ALTER COLUMN [UserName] nvarchar(max) NOT NULL;

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PhoneNumber');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Users] ALTER COLUMN [PhoneNumber] nvarchar(max) NOT NULL;

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(max) NOT NULL;

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'LastName');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Users] ALTER COLUMN [LastName] nvarchar(max) NOT NULL;

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Identification');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Users] ALTER COLUMN [Identification] nvarchar(max) NOT NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'FirstName');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Users] ALTER COLUMN [FirstName] nvarchar(max) NOT NULL;

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(max) NOT NULL;

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreationDate');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var9 + '];');

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'BirthDate');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Users] ALTER COLUMN [BirthDate] datetime2 NOT NULL;

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Active');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var11 + '];');

ALTER TABLE [Users] ADD [CompanyId] int NOT NULL DEFAULT 0;

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'ShortDescription');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Products] ALTER COLUMN [ShortDescription] nvarchar(max) NOT NULL;

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Reference');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Products] ALTER COLUMN [Reference] nvarchar(max) NOT NULL;

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'InvoiceWithoutStock');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var14 + '];');

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Cost');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [Products] ALTER COLUMN [Cost] float NOT NULL;

ALTER TABLE [Products] ADD [CompanyId] int NOT NULL DEFAULT 0;

ALTER TABLE [Products] ADD [ReservedStock] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Products] ADD [Stock] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Categories] ADD [CompanyId] int NOT NULL DEFAULT 0;

CREATE TABLE [CashMovements] (
    [Id] int NOT NULL IDENTITY,
    [CashRegisterId] int NOT NULL,
    [Description] nvarchar(max) NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CashMovements] PRIMARY KEY ([Id])
);

CREATE TABLE [CashRegisters] (
    [Id] int NOT NULL IDENTITY,
    [OpeningAmount] decimal(18,2) NOT NULL,
    [ClosingAmount] decimal(18,2) NULL,
    [OpenedAt] datetime2 NOT NULL,
    [ClosedAt] datetime2 NULL,
    [CompanyId] int NOT NULL,
    [IsOpen] bit NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CashRegisters] PRIMARY KEY ([Id])
);

CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [IsGeneric] bit NOT NULL,
    [CurrentDebt] decimal(18,2) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] int NOT NULL,
    [Reference] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
);

CREATE TABLE [Purchases] (
    [Id] int NOT NULL IDENTITY,
    [SupplierId] int NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([Id])
);

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [Expires] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Returns] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [TotalReturned] decimal(18,2) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Returns] PRIMARY KEY ([Id])
);

CREATE TABLE [Sales] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NULL,
    [Total] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [PaymentType] int NOT NULL,
    [DueDate] datetime2 NULL,
    [CashRegisterId] int NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Sales] PRIMARY KEY ([Id])
);

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);

CREATE TABLE [CompanySettings] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CreditDaysLimit] int NOT NULL,
    [BlockSalesIfOverdue] bit NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CompanySettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanySettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PurchaseDetails] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Cost] decimal(18,2) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PurchaseDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseDetails_Purchases_PurchaseId] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchases] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ReturnDetails] (
    [Id] int NOT NULL IDENTITY,
    [ReturnId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ReturnDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReturnDetails_Returns_ReturnId] FOREIGN KEY ([ReturnId]) REFERENCES [Returns] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SaleDetails] (
    [Id] int NOT NULL IDENTITY,
    [SaleId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SaleDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleDetails_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_CompanySettings_CompanyId] ON [CompanySettings] ([CompanyId]);

CREATE INDEX [IX_PurchaseDetails_PurchaseId] ON [PurchaseDetails] ([PurchaseId]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE INDEX [IX_ReturnDetails_ReturnId] ON [ReturnDetails] ([ReturnId]);

CREATE INDEX [IX_SaleDetails_SaleId] ON [SaleDetails] ([SaleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260707154807_CreateRestingTables', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260707174126_ArreglandoEsoMimo', N'9.0.0');

CREATE TABLE [Inventories] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id])
);

CREATE TABLE [Movements] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [FromWarehouseId] int NULL,
    [ToWarehouseId] int NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Type] int NOT NULL,
    [CompanyId] int NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Movements] PRIMARY KEY ([Id])
);

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260707225757_Warehouses', N'9.0.0');

ALTER TABLE [Users] ADD [Role] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [RefreshTokens] ADD [DeviceId] nvarchar(max) NULL;

ALTER TABLE [RefreshTokens] ADD [ReplacedByToken] nvarchar(max) NULL;

CREATE TABLE [Invitations] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(max) NOT NULL,
    [CompanyId] int NOT NULL,
    [InvitedByUserId] int NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [Accepted] bit NOT NULL,
    [AcceptedByUserId] int NULL,
    [AcceptedAt] datetime2 NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Invitations] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260708062033_eose', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260708063152_AddRefreshTokenHashAndLastUsed', N'9.0.0');

ALTER TABLE [Sales] ADD [Date] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Sales] ADD [InvoiceFolio] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Customers] ADD [Address] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [Email] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [Identification] nvarchar(max) NULL;

ALTER TABLE [Customers] ADD [Notes] nvarchar(max) NULL;

CREATE TABLE [InventoryMovements] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Type] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [WarehouseId] int NOT NULL,
    [PerformedByUserId] int NOT NULL,
    [OccurredAt] datetime2 NOT NULL,
    [Reference] nvarchar(max) NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id])
);

CREATE TABLE [InvoiceSequences] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [LastFolio] bigint NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_InvoiceSequences] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260708180446_Module345Changes', N'9.0.0');

ALTER TABLE [CompanySettings] ADD [CommercialName] nvarchar(max) NULL;

ALTER TABLE [CompanySettings] ADD [Currency] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [CompanySettings] ADD [DefaultStockAlertThreshold] int NOT NULL DEFAULT 0;

ALTER TABLE [CompanySettings] ADD [DefaultTaxPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [CompanySettings] ADD [InvoiceNumberFormat] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [CompanySettings] ADD [LogoUrl] nvarchar(max) NULL;

ALTER TABLE [CompanySettings] ADD [TimeZone] nvarchar(max) NOT NULL DEFAULT N'';

CREATE TABLE [AccountReceivables] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CustomerId] int NULL,
    [SaleId] int NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AccountReceivables] PRIMARY KEY ([Id])
);

CREATE TABLE [CompanySubscriptions] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [SubscriptionPlanId] int NOT NULL,
    [ExternalCustomerId] nvarchar(max) NULL,
    [ExternalSubscriptionId] nvarchar(max) NULL,
    [StartDate] datetime2 NOT NULL,
    [RenewalDate] datetime2 NULL,
    [Status] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CompanySubscriptions] PRIMARY KEY ([Id])
);

CREATE TABLE [SubscriptionPlans] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [MaxUsers] int NOT NULL,
    [MaxWarehouses] int NOT NULL,
    [MaxProducts] int NOT NULL,
    [MaxSalesPerMonth] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Features] nvarchar(max) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260708192417_Module7to10', N'9.0.0');

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Role');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [Users] ALTER COLUMN [Role] nvarchar(max) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260709140955_SetUserRoleNVarChar50', N'9.0.0');

CREATE TABLE [Appointments] (
    [Id] int NOT NULL IDENTITY,
    [StartAt] datetime2 NOT NULL,
    [EndAt] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [CustomerId] int NULL,
    [ServiceId] int NULL,
    [ResourceId] int NULL,
    [Notes] nvarchar(1000) NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id])
);

CREATE TABLE [Availabilities] (
    [Id] int NOT NULL IDENTITY,
    [ResourceId] int NOT NULL,
    [StartAt] datetime2 NOT NULL,
    [EndAt] datetime2 NOT NULL,
    [IsBlocked] bit NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Availabilities] PRIMARY KEY ([Id])
);

CREATE TABLE [CreditPayments] (
    [Id] int NOT NULL IDENTITY,
    [CreditId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaidAt] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CreditPayments] PRIMARY KEY ([Id])
);

CREATE TABLE [Credits] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [MinimumPaymentAmount] decimal(18,2) NULL,
    [PaymentFrequency] nvarchar(max) NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Credits] PRIMARY KEY ([Id])
);

CREATE TABLE [CreditStatusHistory] (
    [Id] int NOT NULL IDENTITY,
    [CreditId] int NOT NULL,
    [OldStatus] nvarchar(max) NOT NULL,
    [NewStatus] nvarchar(max) NOT NULL,
    [ChangedAt] datetime2 NOT NULL,
    [ChangedByUserId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CreditStatusHistory] PRIMARY KEY ([Id])
);

CREATE TABLE [Resources] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Type] nvarchar(100) NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Resources] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260810151341_AddCreditsAndAppointmentsModules', N'9.0.0');

CREATE INDEX [IX_Resources_CompanyId] ON [Resources] ([CompanyId]);

CREATE INDEX [IX_CreditStatusHistory_CompanyId] ON [CreditStatusHistory] ([CompanyId]);

CREATE INDEX [IX_CreditStatusHistory_CreditId] ON [CreditStatusHistory] ([CreditId]);

CREATE INDEX [IX_Credits_CompanyId] ON [Credits] ([CompanyId]);

CREATE INDEX [IX_CreditPayments_CompanyId] ON [CreditPayments] ([CompanyId]);

CREATE INDEX [IX_CreditPayments_CreditId] ON [CreditPayments] ([CreditId]);

CREATE INDEX [IX_Availabilities_CompanyId] ON [Availabilities] ([CompanyId]);

CREATE INDEX [IX_Availabilities_ResourceId] ON [Availabilities] ([ResourceId]);

CREATE INDEX [IX_Appointments_CompanyId] ON [Appointments] ([CompanyId]);

CREATE INDEX [IX_Appointments_ResourceId] ON [Appointments] ([ResourceId]);

ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Availabilities] ADD CONSTRAINT [FK_Availabilities_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [CreditPayments] ADD CONSTRAINT [FK_CreditPayments_Credits_CreditId] FOREIGN KEY ([CreditId]) REFERENCES [Credits] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [CreditStatusHistory] ADD CONSTRAINT [FK_CreditStatusHistory_Credits_CreditId] FOREIGN KEY ([CreditId]) REFERENCES [Credits] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260810152550_AddCreditsAppointmentsForeignKeys', N'9.0.0');

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CompanyId');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [Users] ALTER COLUMN [CompanyId] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260810175147_MakeUserCompanyNullable', N'9.0.0');

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Identification');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [Users] ALTER COLUMN [Identification] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260810180237_MakeIdentificationNullable', N'9.0.0');

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Cost');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [Products] ALTER COLUMN [Cost] decimal(18,2) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819160103_ChangeProductCostToDecimal', N'9.0.0');

ALTER TABLE [Sales] ADD [CashSessionId] int NULL;

ALTER TABLE [Sales] ADD [ExternalReference] nvarchar(max) NULL;

ALTER TABLE [Sales] ADD [IdempotencyKey] nvarchar(max) NULL;

ALTER TABLE [Sales] ADD [Notes] nvarchar(max) NULL;

ALTER TABLE [Sales] ADD [RowVersion] varbinary(max) NULL;

ALTER TABLE [Sales] ADD [SubTotal] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [Sales] ADD [Tax] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE [PaymentPlans] (
    [Id] int NOT NULL IDENTITY,
    [AccountReceivableId] int NOT NULL,
    [InstallmentAmount] decimal(18,2) NOT NULL,
    [TotalInstallments] int NOT NULL,
    [Frequency] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PaymentPlans] PRIMARY KEY ([Id])
);

CREATE TABLE [ProductTypes] (
    [Id] int NOT NULL IDENTITY,
    [Description] nvarchar(100) NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ProductTypes] PRIMARY KEY ([Id])
);

CREATE TABLE [Installments] (
    [Id] int NOT NULL IDENTITY,
    [PaymentPlanId] int NOT NULL,
    [Number] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Installments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Installments_PaymentPlans_PaymentPlanId] FOREIGN KEY ([PaymentPlanId]) REFERENCES [PaymentPlans] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Installments_PaymentPlanId] ON [Installments] ([PaymentPlanId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260902182640_AddCajaColumnsAndPlans', N'9.0.0');

UPDATE [Categories] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;


UPDATE [Categories] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


UPDATE [Categories] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


UPDATE [Categories] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 4;
SELECT @@ROWCOUNT;


UPDATE [Categories] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 5;
SELECT @@ROWCOUNT;


UPDATE [ProductTypes] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;


UPDATE [ProductTypes] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


UPDATE [ProductTypes] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


UPDATE [ProductTypes] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 4;
SELECT @@ROWCOUNT;


UPDATE [ProductTypes] SET [CreationDate] = '2026-01-01T00:00:00.0000000Z'
WHERE [Id] = 5;
SELECT @@ROWCOUNT;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260902183849_FixSeedDates', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260902185440_FixSeedDates2', N'9.0.0');

ALTER TABLE [PurchaseDetails] ADD [QuantityReceived] decimal(18,2) NOT NULL DEFAULT 0.0;

CREATE TABLE [CreditNotes] (
    [Id] int NOT NULL IDENTITY,
    [Number] nvarchar(max) NOT NULL,
    [Date] datetime2 NOT NULL,
    [CompanyId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CreditNotes] PRIMARY KEY ([Id])
);

CREATE TABLE [ManageRequests] (
    [Id] int NOT NULL IDENTITY,
    [Type] int NOT NULL,
    [PayloadJson] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [Comment] nvarchar(max) NULL,
    [CompanyId] int NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ManageRequests] PRIMARY KEY ([Id])
);

CREATE TABLE [PurchaseOrderReceipts] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [StatusId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderReceipts] PRIMARY KEY ([Id])
);

CREATE TABLE [CreditNoteDetails] (
    [Id] int NOT NULL IDENTITY,
    [CreditNoteId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CreditNoteDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CreditNoteDetails_CreditNotes_CreditNoteId] FOREIGN KEY ([CreditNoteId]) REFERENCES [CreditNotes] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ManageRequestTimelines] (
    [Id] int NOT NULL IDENTITY,
    [ManageRequestId] int NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [User] nvarchar(max) NOT NULL,
    [Comment] nvarchar(max) NULL,
    [OldStatus] int NOT NULL,
    [NewStatus] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ManageRequestTimelines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ManageRequestTimelines_ManageRequests_ManageRequestId] FOREIGN KEY ([ManageRequestId]) REFERENCES [ManageRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PurchaseOrderReceiptDetails] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderReceiptId] int NOT NULL,
    [ProductId] int NOT NULL,
    [QuantityReceived] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [WarehouseId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_PurchaseOrderReceiptDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderReceiptDetails_PurchaseOrderReceipts_PurchaseOrderReceiptId] FOREIGN KEY ([PurchaseOrderReceiptId]) REFERENCES [PurchaseOrderReceipts] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_CreditNoteDetails_CreditNoteId] ON [CreditNoteDetails] ([CreditNoteId]);

CREATE INDEX [IX_ManageRequestTimelines_ManageRequestId] ON [ManageRequestTimelines] ([ManageRequestId]);

CREATE INDEX [IX_PurchaseOrderReceiptDetails_PurchaseOrderReceiptId] ON [PurchaseOrderReceiptDetails] ([PurchaseOrderReceiptId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260903181828_AddCreditNotesModule', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260903182500_WireCreditNoteStockAndCash', N'9.0.0');

ALTER TABLE [Users] ADD [LastLoginAt] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260903192755_AddUserLastLoginAt', N'9.0.0');

IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AuditLogs', N'Active') IS NULL
        ALTER TABLE [dbo].[AuditLogs] ADD [Active] bit NOT NULL CONSTRAINT [DF_AuditLogs_Active] DEFAULT (0);
    IF COL_LENGTH(N'dbo.AuditLogs', N'IsDeleted') IS NULL
        ALTER TABLE [dbo].[AuditLogs] ADD [IsDeleted] bit NOT NULL CONSTRAINT [DF_AuditLogs_IsDeleted] DEFAULT (0);
END

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260904205151_AddAuditLogBaseFields', N'9.0.0');

ALTER TABLE [Companies] ADD [Email] nvarchar(max) NULL;

ALTER TABLE [Companies] ADD [Rnc] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260904211005_AddCompanyRncAndEmail', N'9.0.0');

IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Users', N'IsSuperUser') IS NULL
    ALTER TABLE [dbo].[Users] ADD [IsSuperUser] bit NOT NULL CONSTRAINT [DF_Users_IsSuperUser] DEFAULT (0);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260904211557_NombreMigracion', N'9.0.0');


DECLARE @CompanyId int;
DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @PasswordHash nvarchar(max) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';

SELECT @CompanyId = Id FROM Companies WHERE Email = N'demo@cuadreenv.local';

IF @CompanyId IS NULL
BEGIN
    INSERT INTO Companies (Name, Rnc, Email, Address, Phone, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'CuadreEnv Demo', N'DEMO-000000', N'demo@cuadreenv.local', N'Empresa de demostración', N'000-000-0000', @Now, 1, 0, NULL, N'migration', N'migration');
    SET @CompanyId = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.admin@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Admin', NULL, N'M', N'demo.admin@cuadreenv.local', @PasswordHash, N'000-000-0001', '1990-01-01', N'demo.admin', @CompanyId, N'Admin', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.supervisor@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Supervisor', NULL, N'M', N'demo.supervisor@cuadreenv.local', @PasswordHash, N'000-000-0002', '1990-01-02', N'demo.supervisor', @CompanyId, N'Supervisor', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.vendedor@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Vendedor', NULL, N'M', N'demo.vendedor@cuadreenv.local', @PasswordHash, N'000-000-0003', '1990-01-03', N'demo.vendedor', @CompanyId, N'Vendedor', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.cajero@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Cajero', NULL, N'M', N'demo.cajero@cuadreenv.local', @PasswordHash, N'000-000-0004', '1990-01-04', N'demo.cajero', @CompanyId, N'Cajero', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'demo.employee@cuadreenv.local')
    INSERT INTO Users (FirstName, LastName, Identification, Gender, Email, PasswordHash, PhoneNumber, BirthDate, UserName, CompanyId, Role, LastLoginAt, IsSuperUser, CreationDate, Active, IsDeleted, ModificationDate, CreateBy, ModifiedBy)
    VALUES (N'Demo', N'Employee', NULL, N'M', N'demo.employee@cuadreenv.local', @PasswordHash, N'000-000-0005', '1990-01-05', N'demo.employee', @CompanyId, N'Employee', NULL, 0, @Now, 1, 0, NULL, N'migration', N'migration');


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907183237_SeedCuadreEnvDemoUsers', N'9.0.0');

IF OBJECT_ID(N'[dbo].[AuditLogs]', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AuditLogs', N'AuditPayload') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [AuditPayload] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'CompanyId') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [CompanyId] int NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'EntityName') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [EntityName] nvarchar(max) NOT NULL CONSTRAINT [DF_AuditLogs_EntityName] DEFAULT (N'');
    IF COL_LENGTH(N'dbo.AuditLogs', N'IpAddress') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [IpAddress] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'Timestamp') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [Timestamp] datetime2 NOT NULL CONSTRAINT [DF_AuditLogs_Timestamp] DEFAULT ('0001-01-01T00:00:00');
    IF COL_LENGTH(N'dbo.AuditLogs', N'UserAgent') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [UserAgent] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'UserEmail') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [UserEmail] nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'UserId') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [UserId] int NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'UserRole') IS NULL ALTER TABLE [dbo].[AuditLogs] ADD [UserRole] nvarchar(max) NULL;
END

CREATE TABLE [DeletionApprovalRequests] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [EntityType] nvarchar(200) NOT NULL,
    [EntityId] int NOT NULL,
    [RequestedByUserId] int NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [Reason] nvarchar(max) NULL,
    [Status] nvarchar(30) NOT NULL,
    [ReviewedByUserId] int NULL,
    [ReviewedAt] datetime2 NULL,
    [ReviewNotes] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DeletionApprovalRequests] PRIMARY KEY ([Id])
);

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Module] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsSystemRole] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [RolePermissions] (
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_DeletionApprovalRequests_CompanyId_Status] ON [DeletionApprovalRequests] ([CompanyId], [Status]);

CREATE UNIQUE INDEX [IX_Permissions_Module_Action] ON [Permissions] ([Module], [Action]);

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);

CREATE UNIQUE INDEX [IX_Roles_CompanyId_Name] ON [Roles] ([CompanyId], [Name]);

CREATE INDEX [IX_UserRoles_CompanyId_RoleId] ON [UserRoles] ([CompanyId], [RoleId]);

CREATE INDEX [IX_UserRoles_CompanyId_UserId] ON [UserRoles] ([CompanyId], [UserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907194019_AddDynamicRBACAndApprovalWorkflow', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907203330_SeedRbacPracticeData', N'9.0.0');


DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @Hash nvarchar(max) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';
DECLARE @CompanyA int, @CompanyB int, @AdminA int, @AuditA int, @EmployeeA int, @AdminB int, @AuditB int, @EmployeeB int;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE Rnc = N'RBAC-DEMO-A')
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'RBAC Demo Empresa A',N'RBAC-DEMO-A',N'rbac.empresa.a@cuadreenv.local',N'Tenant A',N'000-100-0001',@Now,1,0,NULL,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Companies WHERE Rnc = N'RBAC-DEMO-B')
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'RBAC Demo Empresa B',N'RBAC-DEMO-B',N'rbac.empresa.b@cuadreenv.local',N'Tenant B',N'000-100-0002',@Now,1,0,NULL,N'seed',N'seed');
SELECT @CompanyA=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-A';
SELECT @CompanyB=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-B';

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Sales' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Sales',N'View',N'Consultar ventas',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Sales' AND Action=N'Create') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Sales',N'Create',N'Crear ventas',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Inventory' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Inventory',N'View',N'Consultar inventario',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Inventory' AND Action=N'Edit') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Inventory',N'Edit',N'Editar inventario',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Customers' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Customers',N'View',N'Consultar clientes',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Audit' AND Action=N'View') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Audit',N'View',N'Consultar auditoría',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Audit' AND Action=N'Approve') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Audit',N'Approve',N'Aprobar eliminaciones',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Module=N'Configuration' AND Action=N'Edit') INSERT INTO Permissions (Module,Action,Description,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'Configuration',N'Edit',N'Configurar roles',@Now,1,0,N'seed',N'seed');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Admin') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Admin',N'Administrador del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Audit') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Audit',N'Auditor con aprobación requerida',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Sales Operator') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'RBAC Sales Operator',N'Rol personalizado de ventas',0,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Admin') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Admin',N'Administrador del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Audit') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Audit',N'Auditor con aprobación requerida',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager') INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');

IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.admin.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Admin A',N'M',N'rbac.admin.a@cuadreenv.local',@Hash,N'000-100-0010','1990-01-01',N'rbac.admin.a',@CompanyA,N'Admin',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.audit.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Audit A',N'M',N'rbac.audit.a@cuadreenv.local',@Hash,N'000-100-0011','1990-01-02',N'rbac.audit.a',@CompanyA,N'Audit',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.employee.a@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Employee A',N'M',N'rbac.employee.a@cuadreenv.local',@Hash,N'000-100-0012','1990-01-03',N'rbac.employee.a',@CompanyA,N'Employee',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.admin.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Admin B',N'M',N'rbac.admin.b@cuadreenv.local',@Hash,N'000-100-0020','1990-02-01',N'rbac.admin.b',@CompanyB,N'Admin',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.audit.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Audit B',N'M',N'rbac.audit.b@cuadreenv.local',@Hash,N'000-100-0021','1990-02-02',N'rbac.audit.b',@CompanyB,N'Audit',0,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email=N'rbac.employee.b@cuadreenv.local') INSERT INTO Users (FirstName,LastName,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,IsSuperUser,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (N'RBAC',N'Employee B',N'M',N'rbac.employee.b@cuadreenv.local',@Hash,N'000-100-0022','1990-02-03',N'rbac.employee.b',@CompanyB,N'Employee',0,@Now,1,0,N'seed',N'seed');
SELECT @AdminA=Id FROM Users WHERE Email=N'rbac.admin.a@cuadreenv.local'; SELECT @AuditA=Id FROM Users WHERE Email=N'rbac.audit.a@cuadreenv.local'; SELECT @EmployeeA=Id FROM Users WHERE Email=N'rbac.employee.a@cuadreenv.local'; SELECT @AdminB=Id FROM Users WHERE Email=N'rbac.admin.b@cuadreenv.local'; SELECT @AuditB=Id FROM Users WHERE Email=N'rbac.audit.b@cuadreenv.local'; SELECT @EmployeeB=Id FROM Users WHERE Email=N'rbac.employee.b@cuadreenv.local';

INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AdminA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name=N'RBAC Admin' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AdminA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AuditA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name=N'RBAC Audit' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AuditA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @EmployeeA,r.Id,@CompanyA,@Now,@AdminA FROM Roles r WHERE r.CompanyId=@CompanyA AND r.Name IN (N'RBAC Sales Operator',N'RBAC Audit') AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@EmployeeA AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AdminB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name=N'RBAC Admin' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AdminB AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @AuditB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name=N'RBAC Audit' AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@AuditB AND x.RoleId=r.Id);
INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) SELECT @EmployeeB,r.Id,@CompanyB,@Now,@AdminB FROM Roles r WHERE r.CompanyId=@CompanyB AND r.Name IN (N'RBAC Inventory Manager',N'RBAC Audit') AND NOT EXISTS (SELECT 1 FROM UserRoles x WHERE x.UserId=@EmployeeB AND x.RoleId=r.Id);

INSERT INTO Products (Description,Barcode,ShortDescription,Reference,MaximumQuantity,MinimumQuantity,ProductTypeId,CategoryId,CompanyId,UnitOfMeasurementId,InvoiceWithoutStock,Cost,Stock,ReservedStock,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) SELECT N'Producto RBAC A',N'RBAC-A-001',N'Demo A',N'RBAC-A-001',100,5,1,1,@CompanyA,1,0,10,25,0,@Now,1,0,N'seed',N'seed' WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Barcode=N'RBAC-A-001');
INSERT INTO Products (Description,Barcode,ShortDescription,Reference,MaximumQuantity,MinimumQuantity,ProductTypeId,CategoryId,CompanyId,UnitOfMeasurementId,InvoiceWithoutStock,Cost,Stock,ReservedStock,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) SELECT N'Producto RBAC B',N'RBAC-B-001',N'Demo B',N'RBAC-B-001',100,5,1,1,@CompanyB,1,0,20,30,0,@Now,1,0,N'seed',N'seed' WHERE NOT EXISTS (SELECT 1 FROM Products WHERE Barcode=N'RBAC-B-001');
DECLARE @ProductA int, @ProductB int; SELECT @ProductA=Id FROM Products WHERE Barcode=N'RBAC-A-001'; SELECT @ProductB=Id FROM Products WHERE Barcode=N'RBAC-B-001';
IF NOT EXISTS (SELECT 1 FROM DeletionApprovalRequests WHERE CompanyId=@CompanyA AND EntityType=N'Product' AND EntityId=@ProductA AND Status=N'Pending') INSERT INTO DeletionApprovalRequests (CompanyId,EntityType,EntityId,RequestedByUserId,RequestedAt,Reason,Status,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyA,N'Product',@ProductA,@AuditA,@Now,N'Validar aprobación de borrado en tenant A',N'Pending',@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM DeletionApprovalRequests WHERE CompanyId=@CompanyB AND EntityType=N'Product' AND EntityId=@ProductB AND Status=N'Pending') INSERT INTO DeletionApprovalRequests (CompanyId,EntityType,EntityId,RequestedByUserId,RequestedAt,Reason,Status,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy) VALUES (@CompanyB,N'Product',@ProductB,@AuditB,@Now,N'Validar aprobación de borrado en tenant B',N'Pending',@Now,1,0,N'seed',N'seed');


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907205247_SeedRbacPracticeRows', N'9.0.0');


DECLARE @RoleId int;
DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT Id FROM Roles WHERE CompanyId IN (SELECT Id FROM Companies WHERE Rnc IN (N'RBAC-DEMO-A',N'RBAC-DEMO-B'));
OPEN role_cursor;
FETCH NEXT FROM role_cursor INTO @RoleId;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Admin')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Audit')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module IN (N'Sales',N'Inventory',N'Customers',N'Audit') AND p.Action IN (N'View',N'Edit',N'Approve') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Sales Operator')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module=N'Sales' AND p.Action IN (N'View',N'Create') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    ELSE IF EXISTS (SELECT 1 FROM Roles WHERE Id=@RoleId AND Name=N'RBAC Inventory Manager')
        INSERT INTO RolePermissions(RoleId,PermissionId)
        SELECT @RoleId,p.Id FROM Permissions p WHERE p.Module=N'Inventory' AND p.Action IN (N'View',N'Edit') AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleId AND x.PermissionId=p.Id);
    FETCH NEXT FROM role_cursor INTO @RoleId;
END;
CLOSE role_cursor;
DEALLOCATE role_cursor;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907205639_SeedRbacRolePermissions', N'9.0.0');

ALTER TABLE [Suppliers] ADD [Address] nvarchar(max) NULL;

ALTER TABLE [Suppliers] ADD [ContactName] nvarchar(max) NULL;

ALTER TABLE [Suppliers] ADD [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Suppliers] ADD [RncOrId] nvarchar(max) NULL;

ALTER TABLE [Products] ADD [IsOrganic] bit NOT NULL DEFAULT CAST(0 AS bit);

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'SaleId');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [Payments] ALTER COLUMN [SaleId] int NULL;

ALTER TABLE [Payments] ADD [AccountPayableId] int NULL;

ALTER TABLE [Payments] ADD [AccountReceivableId] int NULL;

ALTER TABLE [Payments] ADD [CompanyId] int NOT NULL DEFAULT 0;

ALTER TABLE [CashRegisters] ADD [ClosedByUserId] int NULL;

ALTER TABLE [CashRegisters] ADD [DifferenceAmount] decimal(18,2) NULL;

ALTER TABLE [CashRegisters] ADD [InitialAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [CashRegisters] ADD [OpenedByUserId] int NULL;

ALTER TABLE [CashRegisters] ADD [PauseReason] nvarchar(max) NULL;

ALTER TABLE [CashRegisters] ADD [PausedAt] datetime2 NULL;

ALTER TABLE [CashRegisters] ADD [PausedByUserId] int NULL;

ALTER TABLE [CashRegisters] ADD [Status] int NOT NULL DEFAULT 0;

CREATE TABLE [AccountPayables] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [PurchaseId] int NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_AccountPayables] PRIMARY KEY ([Id])
);

CREATE TABLE [CashRegisterPauses] (
    [Id] int NOT NULL IDENTITY,
    [CashRegisterId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [UserId] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [PausedAt] datetime2 NOT NULL,
    [ResumedAt] datetime2 NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_CashRegisterPauses] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908195008_AddOperationalMetricsAndCashLifecycle', N'9.0.0');

ALTER TABLE [Users] ADD [Address] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [City] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [Country] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [IpAddress] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [LastLoginIp] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [Latitude] decimal(18,2) NULL;

ALTER TABLE [Users] ADD [Longitude] decimal(18,2) NULL;

ALTER TABLE [Users] ADD [OperatingLocation] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908211720_AddUserGeolocationAndLoginIp', N'9.0.0');


DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @Hash nvarchar(500) = N'$2a$11$0vkPUpySVVVe36tEDS1lneBomhzRhTmbzzjT.shJokZJ/ajRXL.uq';
DECLARE @CompanyA int, @CompanyB int;

SELECT @CompanyA = Id FROM Companies WHERE Rnc = N'RBAC-DEMO-A';
SELECT @CompanyB = Id FROM Companies WHERE Rnc = N'RBAC-DEMO-B';

IF @CompanyA IS NULL
BEGIN
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'CuadreEnv Demo A',N'RBAC-DEMO-A',N'demo.a@cuadreenv.local',N'Santo Domingo',N'000-100-0001',@Now,1,0,NULL,N'seed',N'seed');
    SET @CompanyA = CONVERT(int, SCOPE_IDENTITY());
END;

IF @CompanyB IS NULL
BEGIN
    INSERT INTO Companies (Name,Rnc,Email,Address,Phone,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
    VALUES (N'CuadreEnv Demo B',N'RBAC-DEMO-B',N'demo.b@cuadreenv.local',N'Santiago',N'000-100-0002',@Now,1,0,NULL,N'seed',N'seed');
    SET @CompanyB = CONVERT(int, SCOPE_IDENTITY());
END;

IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Employee')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyA,N'RBAC Employee',N'Operador básico del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Employee')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyB,N'RBAC Employee',N'Operador básico del tenant',1,@Now,NULL,@Now,1,0,N'seed',N'seed');

INSERT INTO Users (FirstName,LastName,Identification,Gender,Email,PasswordHash,PhoneNumber,BirthDate,UserName,CompanyId,Role,LastLoginAt,Address,City,Country,OperatingLocation,IpAddress,Latitude,Longitude,LastLoginIp,IsSuperUser,CreationDate,Active,IsDeleted,ModificationDate,CreateBy,ModifiedBy)
SELECT v.FirstName,v.LastName,v.Identification,v.Gender,v.Email,@Hash,v.PhoneNumber,v.BirthDate,v.UserName,v.CompanyId,v.LegacyRole,NULL,v.Address,v.City,N'República Dominicana',v.OperatingLocation,NULL,v.Latitude,v.Longitude,NULL,0,@Now,1,0,NULL,N'seed',N'seed'
FROM (VALUES
 (N'Ana',N'Administrador',N'RBAC-A-001',N'F',N'hier.admin.a@cuadreenv.local',N'000-200-0001',CONVERT(date,'1988-01-15'),N'hier.admin.a',@CompanyA,N'Admin',N'Oficina principal',N'Santo Domingo',N'Sede administrativa',18.4861,-69.9312),
 (N'Luis',N'Administrador',N'RBAC-A-002',N'M',N'hier.admin.b@cuadreenv.local',N'000-200-0002',CONVERT(date,'1987-02-20'),N'hier.admin.b',@CompanyB,N'Admin',N'Oficina central',N'Santiago',N'Sede administrativa',19.4517,-70.6970),
 (N'Marta',N'Auditora',N'RBAC-A-003',N'F',N'hier.audit.a@cuadreenv.local',N'000-200-0003',CONVERT(date,'1990-03-10'),N'hier.audit.a',@CompanyA,N'Audit',N'Departamento de auditoría',N'Santo Domingo',N'Oficina de control',18.4861,-69.9312),
 (N'Pedro',N'Auditor',N'RBAC-A-004',N'M',N'hier.audit.b@cuadreenv.local',N'000-200-0004',CONVERT(date,'1991-04-12'),N'hier.audit.b',@CompanyB,N'Audit',N'Departamento de auditoría',N'Santiago',N'Oficina de control',19.4517,-70.6970),
 (N'Carla',N'Ventas',N'RBAC-A-005',N'F',N'hier.sales.a@cuadreenv.local',N'000-200-0005',CONVERT(date,'1993-05-25'),N'hier.sales.a',@CompanyA,N'Employee',N'Punto de ventas 1',N'Santo Domingo',N'Terminal POS 01',18.4861,-69.9312),
 (N'Jorge',N'Ventas',N'RBAC-A-006',N'M',N'hier.sales.b@cuadreenv.local',N'000-200-0006',CONVERT(date,'1992-06-18'),N'hier.sales.b',@CompanyB,N'Employee',N'Punto de ventas 1',N'Santiago',N'Terminal POS 01',19.4517,-70.6970),
 (N'Elena',N'Inventario',N'RBAC-A-007',N'F',N'hier.inventory.a@cuadreenv.local',N'000-200-0007',CONVERT(date,'1989-07-08'),N'hier.inventory.a',@CompanyA,N'Employee',N'Almacén principal',N'Santo Domingo',N'Terminal almacén 01',18.4861,-69.9312),
 (N'Rafael',N'Inventario',N'RBAC-A-008',N'M',N'hier.inventory.b@cuadreenv.local',N'000-200-0008',CONVERT(date,'1988-08-14'),N'hier.inventory.b',@CompanyB,N'Employee',N'Almacén principal',N'Santiago',N'Terminal almacén 01',19.4517,-70.6970),
 (N'Sofía',N'Operadora',N'RBAC-A-009',N'F',N'hier.employee.a@cuadreenv.local',N'000-200-0009',CONVERT(date,'1995-09-30'),N'hier.employee.a',@CompanyA,N'Employee',N'Caja principal',N'Santo Domingo',N'Terminal caja 01',18.4861,-69.9312),
 (N'Diego',N'Operador',N'RBAC-A-010',N'M',N'hier.employee.b@cuadreenv.local',N'000-200-0010',CONVERT(date,'1994-10-22'),N'hier.employee.b',@CompanyB,N'Employee',N'Caja principal',N'Santiago',N'Terminal caja 01',19.4517,-70.6970)
) AS v(FirstName,LastName,Identification,Gender,Email,PhoneNumber,BirthDate,UserName,CompanyId,LegacyRole,Address,City,OperatingLocation,Latitude,Longitude)
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.Email=v.Email);

DECLARE @UserId int, @RoleId int, @Email nvarchar(200), @CompanyId int, @RoleName nvarchar(100);
DECLARE user_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT u.Id,u.Email,u.CompanyId,
       CASE
         WHEN u.Email IN (N'hier.admin.a@cuadreenv.local',N'hier.admin.b@cuadreenv.local') THEN N'RBAC Admin'
         WHEN u.Email IN (N'hier.audit.a@cuadreenv.local',N'hier.audit.b@cuadreenv.local') THEN N'RBAC Audit'
         WHEN u.Email IN (N'hier.sales.a@cuadreenv.local',N'hier.sales.b@cuadreenv.local') THEN N'RBAC Sales Operator'
         WHEN u.Email IN (N'hier.inventory.a@cuadreenv.local',N'hier.inventory.b@cuadreenv.local') THEN N'RBAC Inventory Manager'
         ELSE N'RBAC Employee'
       END
FROM Users u WHERE u.Email LIKE N'hier.%@cuadreenv.local';
OPEN user_cursor;
FETCH NEXT FROM user_cursor INTO @UserId,@Email,@CompanyId,@RoleName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyId AND Name=@RoleName;
    IF @RoleId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyId,@Now,NULL);
    FETCH NEXT FROM user_cursor INTO @UserId,@Email,@CompanyId,@RoleName;
END;
CLOSE user_cursor;
DEALLOCATE user_cursor;

DECLARE @RoleCursorId int;
DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM Roles WHERE Name=N'RBAC Employee' AND CompanyId IN (@CompanyA,@CompanyB);
OPEN role_cursor;
FETCH NEXT FROM role_cursor INTO @RoleCursorId;
WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO RolePermissions(RoleId,PermissionId)
    SELECT @RoleCursorId,p.Id FROM Permissions p
    WHERE ((p.Module=N'Sales' AND p.Action=N'View') OR (p.Module=N'Customers' AND p.Action=N'View'))
      AND NOT EXISTS (SELECT 1 FROM RolePermissions x WHERE x.RoleId=@RoleCursorId AND x.PermissionId=p.Id);
    FETCH NEXT FROM role_cursor INTO @RoleCursorId;
END;
CLOSE role_cursor;
DEALLOCATE role_cursor;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908223740_SeedTenHierarchicalDemoUsers', N'9.0.0');


DECLARE @Now datetime2 = GETUTCDATE();
DECLARE @CompanyA int, @CompanyB int;
SELECT @CompanyA=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-A';
SELECT @CompanyB=Id FROM Companies WHERE Rnc=N'RBAC-DEMO-B';

IF @CompanyA IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Inventory Manager')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyA,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');
IF @CompanyB IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager')
    INSERT INTO Roles (CompanyId,Name,Description,IsSystemRole,CreatedAt,CreatedBy,CreationDate,Active,IsDeleted,CreateBy,ModifiedBy)
    VALUES (@CompanyB,N'RBAC Inventory Manager',N'Rol personalizado de inventario',0,@Now,NULL,@Now,1,0,N'seed',N'seed');

DECLARE @UserId int, @CompanyId int, @RoleId int;
SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyA AND Name=N'RBAC Inventory Manager';
SELECT @UserId=Id FROM Users WHERE Email=N'hier.inventory.a@cuadreenv.local';
IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    DELETE FROM UserRoles WHERE UserId=@UserId AND RoleId IN (SELECT Id FROM Roles WHERE CompanyId=@CompanyA AND Name<>N'RBAC Inventory Manager');
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyA,@Now,NULL);
END;

SELECT @RoleId=Id FROM Roles WHERE CompanyId=@CompanyB AND Name=N'RBAC Inventory Manager';
SELECT @UserId=Id FROM Users WHERE Email=N'hier.inventory.b@cuadreenv.local';
IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    DELETE FROM UserRoles WHERE UserId=@UserId AND RoleId IN (SELECT Id FROM Roles WHERE CompanyId=@CompanyB AND Name<>N'RBAC Inventory Manager');
    IF NOT EXISTS (SELECT 1 FROM UserRoles WHERE UserId=@UserId AND RoleId=@RoleId)
        INSERT INTO UserRoles (UserId,RoleId,CompanyId,CreatedAt,CreatedBy) VALUES (@UserId,@RoleId,@CompanyB,@Now,NULL);
END;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908224629_FixHierarchicalDemoUserRoles', N'9.0.0');


UPDATE Users
SET PasswordHash = N'$2a$11$fwsUE3NQ7K5Rl69qFuRedOtzKiFEGOb8gUXQbxU20Dg4TfVcQhR0W',
    ModificationDate = GETUTCDATE(),
    ModifiedBy = N'seed-password-fix'
WHERE Email IN (
    N'hier.admin.a@cuadreenv.local',
    N'hier.admin.b@cuadreenv.local',
    N'hier.audit.a@cuadreenv.local',
    N'hier.audit.b@cuadreenv.local',
    N'hier.sales.a@cuadreenv.local',
    N'hier.sales.b@cuadreenv.local',
    N'hier.inventory.a@cuadreenv.local',
    N'hier.inventory.b@cuadreenv.local',
    N'hier.employee.a@cuadreenv.local',
    N'hier.employee.b@cuadreenv.local'
);


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260908225249_FixHierarchicalDemoPasswords', N'9.0.0');

CREATE TABLE [ClerkOrganizations] (
    [ClerkOrganizationId] nvarchar(64) NOT NULL,
    [Name] nvarchar(250) NOT NULL,
    [Slug] nvarchar(250) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClerkOrganizations] PRIMARY KEY ([ClerkOrganizationId])
);

CREATE TABLE [ClerkUsers] (
    [ClerkUserId] nvarchar(64) NOT NULL,
    [Email] nvarchar(320) NULL,
    [FirstName] nvarchar(150) NULL,
    [LastName] nvarchar(150) NULL,
    [ImageUrl] nvarchar(2048) NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClerkUsers] PRIMARY KEY ([ClerkUserId])
);

CREATE TABLE [Proyectos] (
    [Id] bigint NOT NULL IDENTITY,
    [OrganizationId] nvarchar(64) NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [Descripcion] nvarchar(2000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Proyectos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Proyectos_ClerkOrganizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [ClerkOrganizations] ([ClerkOrganizationId]) ON DELETE NO ACTION
);

CREATE TABLE [ClerkOrganizationMembers] (
    [ClerkUserId] nvarchar(64) NOT NULL,
    [ClerkOrganizationId] nvarchar(64) NOT NULL,
    [Role] nvarchar(100) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClerkOrganizationMembers] PRIMARY KEY ([ClerkUserId], [ClerkOrganizationId]),
    CONSTRAINT [FK_ClerkOrganizationMembers_ClerkOrganizations_ClerkOrganizationId] FOREIGN KEY ([ClerkOrganizationId]) REFERENCES [ClerkOrganizations] ([ClerkOrganizationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_ClerkOrganizationMembers_ClerkUsers_ClerkUserId] FOREIGN KEY ([ClerkUserId]) REFERENCES [ClerkUsers] ([ClerkUserId]) ON DELETE CASCADE
);

CREATE INDEX [IX_ClerkOrganizationMembers_ClerkOrganizationId] ON [ClerkOrganizationMembers] ([ClerkOrganizationId]);

CREATE INDEX [IX_Proyectos_OrganizationId] ON [Proyectos] ([OrganizationId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260910113723_AddClerkSaasMirror', N'9.0.0');

ALTER TABLE [Products] ADD [RowVersion] rowversion NOT NULL;

ALTER TABLE [InvoiceSequences] ADD [RowVersion] rowversion NOT NULL;

ALTER TABLE [CashRegisters] ADD [ExpectedAmount] decimal(18,2) NULL;

ALTER TABLE [CashRegisters] ADD [IsImmutable] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [CashRegisters] ADD [PhysicalCountAmount] decimal(18,2) NULL;

ALTER TABLE [CashRegisters] ADD [PhysicalCountBreakdownJson] nvarchar(max) NULL;

ALTER TABLE [CashRegisters] ADD [RowVersion] rowversion NOT NULL;

ALTER TABLE [CashMovements] ADD [ApprovedByUserId] int NULL;

ALTER TABLE [CashMovements] ADD [Reason] nvarchar(500) NOT NULL DEFAULT N'';

ALTER TABLE [CashMovements] ADD [RecordedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME());

ALTER TABLE [CashMovements] ADD [RowVersion] rowversion NOT NULL;

CREATE TABLE [FiscalDocuments] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [SaleId] int NOT NULL,
    [DocumentKey] nvarchar(200) NOT NULL,
    [Ncf] nvarchar(30) NULL,
    [EcfTrackId] nvarchar(200) NULL,
    [Status] int NOT NULL,
    [AttemptCount] int NOT NULL,
    [LastAttemptAt] datetime2 NULL,
    [LastError] nvarchar(max) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FiscalDocuments] PRIMARY KEY ([Id])
);

CREATE TABLE [FiscalSubmissionAudits] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [FiscalDocumentId] int NOT NULL,
    [OccurredAt] datetime2 NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [ResponsePayload] nvarchar(max) NULL,
    [PayloadHash] nvarchar(128) NULL,
    [CreationDate] datetime2 NOT NULL,
    [Active] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    [ModificationDate] datetime2 NULL,
    [CreateBy] nvarchar(max) NOT NULL,
    [ModifiedBy] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FiscalSubmissionAudits] PRIMARY KEY ([Id])
);

CREATE INDEX [IX_Products_CompanyId_Stock_MinimumQuantity] ON [Products] ([CompanyId], [Stock], [MinimumQuantity]);

CREATE UNIQUE INDEX [IX_InvoiceSequences_CompanyId] ON [InvoiceSequences] ([CompanyId]);

CREATE INDEX [IX_CashRegisters_CompanyId_Status] ON [CashRegisters] ([CompanyId], [Status]);

CREATE INDEX [IX_CashMovements_CompanyId_CashRegisterId_RecordedAt] ON [CashMovements] ([CompanyId], [CashRegisterId], [RecordedAt]);

CREATE UNIQUE INDEX [IX_FiscalDocuments_CompanyId_DocumentKey] ON [FiscalDocuments] ([CompanyId], [DocumentKey]);

CREATE INDEX [IX_FiscalDocuments_CompanyId_Status_LastAttemptAt] ON [FiscalDocuments] ([CompanyId], [Status], [LastAttemptAt]);

CREATE INDEX [IX_FiscalSubmissionAudits_CompanyId_FiscalDocumentId_OccurredAt] ON [FiscalSubmissionAudits] ([CompanyId], [FiscalDocumentId], [OccurredAt]);

CREATE TRIGGER dbo.TR_CashRegisters_ImmutableAfterClose
ON dbo.CashRegisters
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted WHERE IsImmutable = 1)
        THROW 51001, 'Closed cash registers are immutable.', 1;
END;

CREATE TRIGGER dbo.TR_CashMovements_AppendOnly
ON dbo.CashMovements
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51002, 'Cash movements are append-only.', 1;
END;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911200449_HardenFinancialIntegrity', N'9.0.0');

DROP INDEX [IX_FiscalDocuments_CompanyId_Status_LastAttemptAt] ON [FiscalDocuments];

ALTER TABLE [FiscalDocuments] ADD [NextAttemptAt] datetime2 NULL;

CREATE INDEX [IX_FiscalDocuments_CompanyId_Status_NextAttemptAt] ON [FiscalDocuments] ([CompanyId], [Status], [NextAttemptAt]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260911202228_AddFiscalRetryScheduling', N'9.0.0');


IF EXISTS(
    SELECT 1 FROM sys.indexes i
    JOIN sys.objects o ON i.object_id = o.object_id
    WHERE i.name = 'IX_Sales_CompanyId_IdempotencyKey_Active' AND o.name = 'Sales')
BEGIN
    EXEC sp_rename N'[Sales].[IX_Sales_CompanyId_IdempotencyKey_Active]', N'IX_Sales_CompanyId_IdempotencyKey_IsDeleted', 'INDEX';
END


DROP INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted] ON [Sales];
DECLARE @var21 sysname;
SELECT @var21 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sales]') AND [c].[name] = N'IdempotencyKey');
IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Sales] DROP CONSTRAINT [' + @var21 + '];');
ALTER TABLE [Sales] ALTER COLUMN [IdempotencyKey] nvarchar(450) NULL;
CREATE UNIQUE INDEX [IX_Sales_CompanyId_IdempotencyKey_IsDeleted] ON [Sales] ([CompanyId], [IdempotencyKey], [IsDeleted]) WHERE [IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925185157_ApplyPendingModelChanges', N'9.0.0');

ALTER TABLE [SaleDetails] ADD [WarehouseId] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260925192757_AddSaleDetailWarehouse', N'9.0.0');

COMMIT;
GO

