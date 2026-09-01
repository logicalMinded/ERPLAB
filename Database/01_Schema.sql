-- =====================================================================
-- 專案名稱：ERPLAB 企業資源規劃系統
-- 檔案名稱：01_Schema.sql
-- 執行順序：1 / 4
-- 核心職責：資料庫重建、建立所有實體資料表、叢集索引與 UDTT。
-- 🚨 警告：本腳本會抹除 ERPLAB2026 所有資料，嚴禁於正式環境執行！
-- =====================================================================

USE [master];
GO

-- 1. 強制踢除所有連線並刪除資料庫，<限開發環境與>與<測試環境>
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ERPLAB2026')
BEGIN
    ALTER DATABASE [ERPLAB2026] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [ERPLAB2026];
END
GO

CREATE DATABASE [ERPLAB2026];
GO

USE [ERPLAB2026];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ========================================================
-- [基礎字典模組]
-- ========================================================

CREATE TABLE [dbo].[Base_City] (
    [CityID]   INT IDENTITY(1,1) NOT NULL,
    [CityNo]   VARCHAR(10)       NOT NULL, 
    [CityName] NVARCHAR(20)      NOT NULL, 
    [SortSeq]  INT               NOT NULL CONSTRAINT DF_Base_City_SortSeq DEFAULT 0, 
    [IsActive] BIT               NOT NULL CONSTRAINT DF_Base_City_IsActive DEFAULT 1, 
    CONSTRAINT [PK_Base_City] PRIMARY KEY CLUSTERED ([CityID] ASC),
    CONSTRAINT [UQ_Base_City_CityNo] UNIQUE NONCLUSTERED ([CityNo]),
    CONSTRAINT [CK_Base_City_SortSeq] CHECK ([SortSeq] >= 0)
);
GO

CREATE TABLE [dbo].[Base_District] (
    [DistrictID]   INT IDENTITY(1,1) NOT NULL,
    [CityID]       INT               NOT NULL, 
    [ZipCode]      VARCHAR(3)        NOT NULL, 
    [DistrictName] NVARCHAR(20)      NOT NULL, 
    [SortSeq]      INT               NOT NULL CONSTRAINT DF_Base_District_SortSeq DEFAULT 0, 
    [IsActive]     BIT               NOT NULL CONSTRAINT DF_Base_District_IsActive DEFAULT 1, 
    CONSTRAINT [PK_Base_District] PRIMARY KEY CLUSTERED ([DistrictID] ASC),
    CONSTRAINT [CK_Base_District_SortSeq] CHECK ([SortSeq] >= 0),
    CONSTRAINT [CK_Base_District_ZipCode] CHECK (LEN([ZipCode]) = 3 AND [ZipCode] NOT LIKE '%[^0-9]%'), 
    CONSTRAINT [UQ_Base_District_City_District] UNIQUE NONCLUSTERED ([CityID], [DistrictName]) 
);
GO

-- ========================================================
-- [權限與帳號模組]
-- ========================================================

CREATE TABLE [dbo].[Permissions] (
    [PermissionCode] VARCHAR(100) NOT NULL, 
    [PermissionName] NVARCHAR(50) NOT NULL, 
    [IsActive]       BIT          NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT 1,
    CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([PermissionCode] ASC)
);
GO

CREATE TABLE [dbo].[SystemNodes] (
    [NodeID]          INT IDENTITY(1,1) NOT NULL,
    [NodeName]        NVARCHAR(50)      NOT NULL, 
    [NodeType]        TINYINT           NOT NULL, 
    [ParentNodeID]    INT                   NULL, 
    [SortSeq]         INT               NOT NULL CONSTRAINT DF_SystemNodes_SortSeq DEFAULT 0, 
    [FormClassPath]   VARCHAR(255)          NULL, 
    [PermissionCode]  VARCHAR(100)      NOT NULL, 
    [IsActive]        BIT               NOT NULL CONSTRAINT DF_SystemNodes_IsActive DEFAULT 1,
    CONSTRAINT [PK_SystemNodes] PRIMARY KEY CLUSTERED ([NodeID] ASC),
    CONSTRAINT [CK_SystemNodes_Structure_Logic] CHECK (
        ([NodeType] = 1 AND [ParentNodeID] IS NULL     AND [FormClassPath] IS NULL) OR
        ([NodeType] = 2 AND [ParentNodeID] IS NOT NULL AND [FormClassPath] IS NOT NULL) OR
        ([NodeType] = 3 AND [ParentNodeID] IS NOT NULL AND [FormClassPath] IS NULL)
    ),
    CONSTRAINT [CK_SystemNodes_NodeType] CHECK ([NodeType] IN (1, 2, 3)),
    CONSTRAINT [CK_SystemNodes_SortSeq] CHECK ([SortSeq] >= 0)
);
GO

CREATE TABLE [dbo].[Roles] (
    [RoleID]        INT IDENTITY(1,1) NOT NULL,
    [RoleCode]      VARCHAR(50)       NOT NULL, 
    [RoleName]      NVARCHAR(50)      NOT NULL, 
    [Description]   NVARCHAR(200)     NOT NULL, 
    [IsSystem]      BIT               NOT NULL CONSTRAINT DF_Roles_IsSystem DEFAULT 0, 
    [IsActive]      BIT               NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT 1,
    [RowVersion]    ROWVERSION        NOT NULL, 
    [DbCreateTime]  DATETIME          NOT NULL CONSTRAINT DF_Roles_DbCreateTime DEFAULT GETDATE(),
    [DbCreateUser]  VARCHAR(100)      NOT NULL CONSTRAINT DF_Roles_DbCreateUser DEFAULT SUSER_SNAME(),
    [DbUpdateTime]  DATETIME          NOT NULL CONSTRAINT DF_Roles_DbUpdateTime DEFAULT GETDATE(),
    [DbUpdateUser]  VARCHAR(100)      NOT NULL CONSTRAINT DF_Roles_DbUpdateUser DEFAULT SUSER_SNAME(),
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleID] ASC),
    CONSTRAINT [UQ_Roles_RoleCode] UNIQUE NONCLUSTERED ([RoleCode]),
    CONSTRAINT [UQ_Roles_RoleName] UNIQUE NONCLUSTERED ([RoleName])
);
GO

CREATE TABLE [dbo].[RolePermissions] (
    [RoleID]         INT          NOT NULL,
    [PermissionCode] VARCHAR(100) NOT NULL,
    [DbCreateTime]   DATETIME     NOT NULL CONSTRAINT DF_RolePermissions_DbCreateTime DEFAULT GETDATE(),
    [DbCreateUser]   VARCHAR(100) NOT NULL CONSTRAINT DF_RolePermissions_DbCreateUser DEFAULT SUSER_SNAME(),
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RoleID] ASC, [PermissionCode] ASC)
);
GO

CREATE TABLE [dbo].[Accounts] (
    [AccountID]    INT IDENTITY(1,1) NOT NULL,
    [EmployeeID]   INT               NOT NULL, 
    [Username]     VARCHAR(50)       NOT NULL,
    [PasswordHash] VARCHAR(255)      NOT NULL, 
    [IsLocked]     BIT               NOT NULL CONSTRAINT DF_Accounts_IsLocked DEFAULT 0,    
    [FailedCount]  TINYINT           NOT NULL CONSTRAINT DF_Accounts_FailedCount DEFAULT 0,  
    [LastLogin]    DATETIME              NULL,     
    [DbCreateTime] DATETIME          NOT NULL CONSTRAINT DF_Accounts_DbCreateTime DEFAULT GETDATE(),
    [DbCreateUser] VARCHAR(100)      NOT NULL CONSTRAINT DF_Accounts_DbCreateUser DEFAULT SUSER_SNAME(),
    [DbUpdateTime] DATETIME          NOT NULL CONSTRAINT DF_Accounts_DbUpdateTime DEFAULT GETDATE(),
    [DbUpdateUser] VARCHAR(100)      NOT NULL CONSTRAINT DF_Accounts_DbUpdateUser DEFAULT SUSER_SNAME(),                                                           
    [IsActive]     BIT               NOT NULL CONSTRAINT DF_Accounts_IsActive DEFAULT 1,
    [RowVersion]   ROWVERSION        NOT NULL, 
    CONSTRAINT [PK_Accounts] PRIMARY KEY CLUSTERED ([AccountID] ASC)
);
GO

CREATE TABLE [dbo].[UserRoles] (
    [AccountID]     INT           NOT NULL,
    [RoleID]        INT           NOT NULL,
    [DbCreateTime]  DATETIME      NOT NULL CONSTRAINT DF_UserRoles_DbCreateTime DEFAULT GETDATE(),
    [DbCreateUser]  VARCHAR(100)  NOT NULL CONSTRAINT DF_UserRoles_DbCreateUser DEFAULT SUSER_SNAME(),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY CLUSTERED ([AccountID] ASC, [RoleID] ASC)
);
GO

-- ========================================================
-- [基本檔模組]
-- ========================================================

CREATE TABLE [dbo].[AutoNumber] (
    [DocType]     VARCHAR(5) NOT NULL,
    [CurrentDate] DATE       NOT NULL CONSTRAINT DF_AutoNumber_CurrentDate DEFAULT GETDATE(),
    [LastSeq]     INT        NOT NULL CONSTRAINT DF_AutoNumber_LastSeq DEFAULT 0,
    CONSTRAINT [PK_AutoNumber] PRIMARY KEY CLUSTERED ([DocType] ASC)
);
GO

CREATE TABLE [dbo].[Customer] (
    [CustomerID]    INT IDENTITY(1,1) NOT NULL,
    [CustomerNo]    VARCHAR(20)       NOT NULL,
    [CustomerName]  NVARCHAR(50)      NOT NULL,
    [TaxID]         CHAR(8)           NULL,
    [Gender]        TINYINT           NOT NULL,
    [PhoneNumber]   VARCHAR(20)       NOT NULL,
    [DistrictID]    INT               NOT NULL, 
    [CustomZipCode] VARCHAR(6)        NOT NULL, 
    [Address]       NVARCHAR(200)     NOT NULL,
    [Email]         VARCHAR(100)      NULL,
    [Interests]     NVARCHAR(500)     NULL,
    [Remark]        NVARCHAR(500)     NULL,
    [ImageName]     VARCHAR(255)      NULL, 
    [CreateTime]    DATETIME          NOT NULL CONSTRAINT DF_Customer_CreateTime DEFAULT GETDATE(),
    [CreateUser]    INT               NOT NULL, 
    [UpdateTime]    DATETIME          NOT NULL CONSTRAINT DF_Customer_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]    INT               NOT NULL, 
    [IsActive]      BIT               NOT NULL CONSTRAINT DF_Customer_IsActive DEFAULT 1,      
    [RowVersion]    ROWVERSION        NOT NULL, 
    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED ([CustomerID] ASC),
    CONSTRAINT [UQ_Customer_CustomerNo] UNIQUE NONCLUSTERED ([CustomerNo]),
    CONSTRAINT [UQ_Customer_PhoneNumber] UNIQUE NONCLUSTERED ([PhoneNumber]),
    CONSTRAINT [CK_Customer_Gender] CHECK ([Gender] IN (0, 1, 2)),
    CONSTRAINT [CK_Customer_CustomZipCode_Length] CHECK (LEN([CustomZipCode]) = 3 OR LEN([CustomZipCode]) = 6), 
    CONSTRAINT [CK_Customer_CustomZipCode_Numeric] CHECK ([CustomZipCode] NOT LIKE '%[^0-9]%'), 
    CONSTRAINT [CK_Customer_PhoneNumber_StrictSymbols] CHECK (LEN([PhoneNumber]) >= 7 AND [PhoneNumber] NOT LIKE '%[^0-9+#-]%'),
    CONSTRAINT [CK_Customer_Email_NullableCheck] CHECK ([Email] IS NULL OR [Email] LIKE '%_@_%._%'),
    CONSTRAINT [CK_Customer_TaxID_Numeric] CHECK ([TaxID] IS NULL OR [TaxID] NOT LIKE '%[^0-9]%')
);
GO

CREATE TABLE [dbo].[Vendor] (
    [VendorID]      INT IDENTITY(1,1) NOT NULL,
    [VendorNo]      VARCHAR(20)       NOT NULL,
    [VendorName]    NVARCHAR(100)     NOT NULL,
    [TaxID]         CHAR(8)           NULL, 
    [ContactPerson] NVARCHAR(50)      NOT NULL,
    [PhoneNumber]   VARCHAR(20)       NOT NULL,
    [DistrictID]    INT               NOT NULL, 
    [CustomZipCode] VARCHAR(6)        NOT NULL, 
    [Address]       NVARCHAR(200)     NOT NULL,
    [Email]         VARCHAR(100)      NULL,
    [Remark]        NVARCHAR(500)     NULL,
    [CreateTime]    DATETIME          NOT NULL CONSTRAINT DF_Vendor_CreateTime DEFAULT GETDATE(),
    [CreateUser]    INT               NOT NULL, 
    [UpdateTime]    DATETIME          NOT NULL CONSTRAINT DF_Vendor_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]    INT               NOT NULL, 
    [IsActive]      BIT               NOT NULL CONSTRAINT DF_Vendor_IsActive DEFAULT 1,      
    [RowVersion]    ROWVERSION        NOT NULL, 
    CONSTRAINT [PK_Vendor] PRIMARY KEY CLUSTERED ([VendorID] ASC),
    CONSTRAINT [UQ_Vendor_VendorNo] UNIQUE NONCLUSTERED ([VendorNo]),
    CONSTRAINT [CK_Vendor_TaxID_Numeric] CHECK ([TaxID] IS NULL OR [TaxID] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_Vendor_CustomZipCode_Length] CHECK (LEN([CustomZipCode]) = 3 OR LEN([CustomZipCode]) = 6), 
    CONSTRAINT [CK_Vendor_CustomZipCode_Numeric] CHECK ([CustomZipCode] NOT LIKE '%[^0-9]%'), 
    CONSTRAINT [CK_Vendor_PhoneNumber_StrictSymbols] CHECK (LEN([PhoneNumber]) >= 7 AND [PhoneNumber] NOT LIKE '%[^0-9+#-]%'),
    CONSTRAINT [CK_Vendor_Email_NullableCheck] CHECK ([Email] IS NULL OR [Email] LIKE '%_@_%._%')
);
GO

CREATE TABLE [dbo].[Employee] (
    [EmployeeID]    INT IDENTITY(1,1) NOT NULL,
    [EmployeeNo]    VARCHAR(20)       NOT NULL,
    [EmployeeName]  NVARCHAR(50)      NOT NULL,
    [IsActive]      BIT               NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT 1,      
    [JobStatus]     TINYINT           NOT NULL CONSTRAINT DF_Employee_JobStatus DEFAULT 2,     
    [JobTitle]      NVARCHAR(50)      NOT NULL,
    [Gender]        TINYINT           NOT NULL, 
    [PhoneNumber]   VARCHAR(20)       NOT NULL,
    [CreateTime]    DATETIME          NOT NULL CONSTRAINT DF_Employee_CreateTime DEFAULT GETDATE(),
    [CreateUser]    INT               NOT NULL, 
    [UpdateTime]    DATETIME          NOT NULL CONSTRAINT DF_Employee_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]    INT               NOT NULL, 
    [RowVersion]    ROWVERSION        NOT NULL, 
    [DistrictID]    INT               NOT NULL,
    [CustomZipCode] VARCHAR(6)        NOT NULL,
    [Address]       NVARCHAR(200)     NOT NULL,
    [Email]         VARCHAR(100)          NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED ([EmployeeID] ASC),
    CONSTRAINT [UQ_Employee_EmployeeNo] UNIQUE NONCLUSTERED ([EmployeeNo]),
    CONSTRAINT [CK_Employee_Gender] CHECK ([Gender] IN (0, 1, 2)),
    CONSTRAINT [CK_Employee_JobStatus] CHECK ([JobStatus] IN (0, 1, 2)), 
    CONSTRAINT [CK_Employee_CustomZipCode_Length] CHECK (LEN([CustomZipCode]) = 3 OR LEN([CustomZipCode]) = 6),
    CONSTRAINT [CK_Employee_CustomZipCode_Numeric] CHECK ([CustomZipCode] NOT LIKE '%[^0-9]%'),
    CONSTRAINT [CK_Employee_Email_NullableCheck] CHECK ([Email] IS NULL OR [Email] LIKE '%_@_%._%'),    
    CONSTRAINT [CK_Employee_PhoneNumber_StrictSymbols] CHECK (LEN([PhoneNumber]) >= 7 AND [PhoneNumber] NOT LIKE '%[^0-9+#-]%')
);
GO

CREATE TABLE [dbo].[Product] (
    [ProductID]         INT IDENTITY(1,1) NOT NULL,
    [ProductNo]         VARCHAR(20)       NOT NULL,
    [ProductName]       NVARCHAR(100)     NOT NULL,
    [PurchasePrice]     DECIMAL(18, 2)    NOT NULL,
    [SalesPrice]        DECIMAL(18, 2)    NOT NULL,
    [CurrentStock]      INT               NOT NULL CONSTRAINT DF_Product_CurrentStock DEFAULT 0,
    [Description]       NVARCHAR(MAX)     NULL,
    [ImageName]         VARCHAR(255)      NULL, 
    [Remark]            NVARCHAR(500)     NULL,
    [CreateTime]        DATETIME          NOT NULL CONSTRAINT DF_Product_CreateTime DEFAULT GETDATE(),
    [CreateUser]        INT               NOT NULL, 
    [UpdateTime]        DATETIME          NOT NULL CONSTRAINT DF_Product_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]        INT               NOT NULL, 
    [IsActive]          BIT               NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT 1,      
    [RowVersion]        ROWVERSION        NOT NULL, 
    [MovingAverageCost] DECIMAL(18, 4)    NOT NULL CONSTRAINT DF_Product_MovingAverageCost DEFAULT 0,
    CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED ([ProductID] ASC),
    CONSTRAINT [UQ_Product_ProductNo] UNIQUE NONCLUSTERED ([ProductNo]),
    CONSTRAINT [CK_Product_PurchasePrice] CHECK ([PurchasePrice] >= 0),
    CONSTRAINT [CK_Product_SalesPrice]    CHECK ([SalesPrice] >= 0),
    CONSTRAINT [CK_Product_CurrentStock]  CHECK ([CurrentStock] >= 0) 
);
GO

-- ========================================================
-- [交易單據模組]
-- ========================================================

CREATE TABLE [dbo].[SalesMaster] (
    [SalesID]        BIGINT IDENTITY(1,1) NOT NULL,
    [SalesNo]        VARCHAR(20)          NOT NULL,
    [SalesDate]      DATETIME             NOT NULL CONSTRAINT DF_SalesMaster_SalesDate DEFAULT GETDATE(),
    [ShipZipCode]    VARCHAR(6)           NOT NULL,
    [ShipAddress]    NVARCHAR(200)        NOT NULL,
    [CustomerID]     INT                  NOT NULL, 
    [TotalAmount]    DECIMAL(18, 2)       NOT NULL CONSTRAINT DF_SalesMaster_TotalAmount DEFAULT 0,
    [Remark]         NVARCHAR(500)        NULL,
    [CreateTime]     DATETIME             NOT NULL CONSTRAINT DF_SalesMaster_CreateTime DEFAULT GETDATE(),
    [CreateUser]     INT                  NOT NULL, 
    [UpdateTime]     DATETIME             NOT NULL CONSTRAINT DF_SalesMaster_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]     INT                  NOT NULL, 
    [Status]         TINYINT              NOT NULL CONSTRAINT DF_SalesMaster_Status DEFAULT 1,    
    [RowVersion]     ROWVERSION           NOT NULL, 
    [ShipDistrictID] INT                  NOT NULL,
    CONSTRAINT [PK_SalesMaster] PRIMARY KEY CLUSTERED ([SalesID] ASC),
    CONSTRAINT [UQ_SalesMaster_SalesNo] UNIQUE NONCLUSTERED ([SalesNo]),
    CONSTRAINT [CK_SalesMaster_TotalAmount] CHECK ([TotalAmount] >= 0),
    CONSTRAINT [CK_SalesMaster_Status] CHECK ([Status] IN (1, 2, 3, 4)), 
    CONSTRAINT [CK_SalesMaster_ShipZipCode_Length] CHECK (LEN([ShipZipCode]) = 3 OR LEN([ShipZipCode]) = 6), 
    CONSTRAINT [CK_SalesMaster_ShipZipCode_Numeric] CHECK ([ShipZipCode] NOT LIKE '%[^0-9]%') 
);
GO

CREATE TABLE [dbo].[SalesDetail] (
    [SalesDID]  BIGINT IDENTITY(1,1) NOT NULL,
    [SalesID]   BIGINT               NOT NULL,
    [LineNo]    INT                  NOT NULL,
    [ProductID] INT                  NOT NULL,
    [UnitPrice] DECIMAL(18, 2)       NOT NULL,
    [Qty]       INT                  NOT NULL,
    [Remark]    NVARCHAR(500)        NULL,
    [UnitCost]  DECIMAL(18, 4)       NOT NULL CONSTRAINT DF_SalesDetail_UnitCost DEFAULT 0,
    CONSTRAINT [PK_SalesDetail] PRIMARY KEY NONCLUSTERED ([SalesDID] ASC), -- 降級為非叢集索引
    CONSTRAINT [CK_SalesDetail_LineNo] CHECK ([LineNo] > 0),
    CONSTRAINT [CK_SalesDetail_UnitPrice] CHECK ([UnitPrice] >= 0), 
    CONSTRAINT [CK_SalesDetail_Qty] CHECK ([Qty] > 0)               
);
GO
-- 確保同單據明細在硬碟上絕對連續
CREATE CLUSTERED INDEX [CX_SalesDetail_Sales_LineNo] ON [dbo].[SalesDetail] ([SalesID] ASC, [LineNo] ASC);
GO

CREATE TABLE [dbo].[PurchaseMaster] (
    [PurchaseID]   BIGINT IDENTITY(1,1) NOT NULL,
    [PurchaseNo]   VARCHAR(20)          NOT NULL,
    [PurchaseDate] DATETIME             NOT NULL CONSTRAINT DF_PurchaseMaster_PurchaseDate DEFAULT GETDATE(),
    [VendorID]     INT                  NOT NULL, 
    [TotalAmount]  DECIMAL(18, 2)       NOT NULL CONSTRAINT DF_PurchaseMaster_TotalAmount DEFAULT 0,
    [Remark]       NVARCHAR(500)        NULL,
    [CreateTime]   DATETIME             NOT NULL CONSTRAINT DF_PurchaseMaster_CreateTime DEFAULT GETDATE(),
    [CreateUser]   INT                  NOT NULL, 
    [UpdateTime]   DATETIME             NOT NULL CONSTRAINT DF_PurchaseMaster_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]   INT                  NOT NULL,  
    [Status]       TINYINT              NOT NULL CONSTRAINT DF_PurchaseMaster_Status DEFAULT 1,         
    [RowVersion]   ROWVERSION           NOT NULL, 
    CONSTRAINT [PK_PurchaseMaster] PRIMARY KEY CLUSTERED ([PurchaseID] ASC),
    CONSTRAINT [UQ_PurchaseMaster_PurchaseNo] UNIQUE NONCLUSTERED ([PurchaseNo]),
    CONSTRAINT [CK_PurchaseMaster_TotalAmount] CHECK ([TotalAmount] >= 0),
    CONSTRAINT [CK_PurchaseMaster_Status] CHECK ([Status] IN (1, 2, 3, 4)) 
);
GO

CREATE TABLE [dbo].[PurchaseDetail] (
    [PurchaseDID] BIGINT IDENTITY(1,1) NOT NULL,
    [PurchaseID]  BIGINT               NOT NULL,
    [LineNo]      INT               NOT NULL,
    [ProductID]   INT                  NOT NULL,
    [UnitPrice]   DECIMAL(18, 2)       NOT NULL,
    [Qty]         INT                  NOT NULL,
    [Remark]      NVARCHAR(500)        NULL,
    CONSTRAINT [PK_PurchaseDetail] PRIMARY KEY NONCLUSTERED ([PurchaseDID] ASC),
    CONSTRAINT [CK_PurchaseDetail_LineNo] CHECK ([LineNo] > 0),
    CONSTRAINT [CK_PurchaseDetail_UnitPrice] CHECK ([UnitPrice] >= 0), 
    CONSTRAINT [CK_PurchaseDetail_Qty] CHECK ([Qty] > 0)               
);
GO
CREATE CLUSTERED INDEX [CX_PurchaseDetail_Purchase_LineNo] ON [dbo].[PurchaseDetail] ([PurchaseID] ASC, [LineNo] ASC);
GO

CREATE TABLE [dbo].[InventoryMaster] (
    [InventoryID]   BIGINT IDENTITY(1,1) NOT NULL,
    [InventoryNo]   VARCHAR(20)          NOT NULL,
    [InventoryDate] DATETIME             NOT NULL CONSTRAINT DF_InventoryMaster_InventoryDate DEFAULT GETDATE(),
    [EmployeeID]    INT                  NOT NULL, 
    [Remark]        NVARCHAR(500)        NULL,
    [CreateTime]    DATETIME             NOT NULL CONSTRAINT DF_InventoryMaster_CreateTime DEFAULT GETDATE(),
    [CreateUser]    INT                  NOT NULL, 
    [UpdateTime]    DATETIME             NOT NULL CONSTRAINT DF_InventoryMaster_UpdateTime DEFAULT GETDATE(),
    [UpdateUser]    INT                  NOT NULL,         
    [Status]        TINYINT              NOT NULL CONSTRAINT DF_InventoryMaster_Status DEFAULT 1,         
    [RowVersion]    ROWVERSION           NOT NULL, 
    CONSTRAINT [PK_InventoryMaster] PRIMARY KEY CLUSTERED ([InventoryID] ASC),
    CONSTRAINT [UQ_InventoryMaster_InventoryNo] UNIQUE NONCLUSTERED ([InventoryNo]),
    CONSTRAINT [CK_InventoryMaster_Status] CHECK ([Status] IN (1, 2))
);
GO

CREATE TABLE [dbo].[InventoryDetail] (
    [InventoryDID]   BIGINT IDENTITY(1,1) NOT NULL,
    [InventoryID]    BIGINT               NOT NULL,
    [LineNo]         INT                  NOT NULL,
    [ProductID]      INT                  NOT NULL,
    [SystemStock]    INT                  NOT NULL, 
    [ActualStock]    INT                  NOT NULL, 
    [StockPrice]     DECIMAL(18, 2)       NOT NULL, 
    [Remark]         NVARCHAR(500)        NULL,
    CONSTRAINT [PK_InventoryDetail] PRIMARY KEY NONCLUSTERED ([InventoryDID] ASC),
    CONSTRAINT [CK_InventoryDetail_LineNo] CHECK ([LineNo] > 0),
    CONSTRAINT [CK_InventoryDetail_SystemStock] CHECK ([SystemStock] >= 0), 
    CONSTRAINT [CK_InventoryDetail_ActualStock] CHECK ([ActualStock] >= 0), 
    CONSTRAINT [CK_InventoryDetail_StockPrice]  CHECK ([StockPrice] >= 0)   
);
GO
CREATE CLUSTERED INDEX [CX_InventoryDetail_Inventory_LineNo] ON [dbo].[InventoryDetail] ([InventoryID] ASC, [LineNo] ASC);
GO

-- ========================================================
-- [使用者定義資料表型別 UDTT] 供 TVP 使用
-- ========================================================

CREATE TYPE [dbo].[SalesDetailType] AS TABLE(
	[LineNo] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[Qty] [int] NOT NULL,
	[Remark] [nvarchar](500) NULL
);
GO

CREATE TYPE [dbo].[PurchaseDetailType] AS TABLE(
	[LineNo] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[Qty] [int] NOT NULL,
	[Remark] [nvarchar](500) NULL
);
GO

CREATE TYPE [dbo].[InventoryDetailType] AS TABLE(
	[LineNo] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[SystemStock] [int] NOT NULL,
	[ActualStock] [int] NOT NULL,
	[StockPrice] [decimal](18, 2) NOT NULL,
	[Remark] [nvarchar](500) NULL
);
GO