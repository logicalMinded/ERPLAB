-- =====================================================================
-- 專案名稱：ERPLAB 企業資源規劃系統
-- 檔案名稱：02_Constraints.sql
-- 執行順序：2 / 4
-- 核心職責：建立所有 Foreign Key (外部鍵約束)、Unique 索引與效能檢索索引。
-- 物理目的：確保 01_Schema 所有實體已建立後，再繫結網狀關聯，避開相依性死結。
-- =====================================================================

USE [ERPLAB2026];
GO

-- ========================================================
-- [基礎字典模組] 外鍵與索引
-- ========================================================
ALTER TABLE [dbo].[Base_District] ADD CONSTRAINT [FK_Base_District_Base_City] 
    FOREIGN KEY ([CityID]) REFERENCES [dbo].[Base_City] ([CityID]) ON DELETE NO ACTION;
GO

-- ========================================================
-- [權限與帳號模組] 外鍵與索引
-- ========================================================
ALTER TABLE [dbo].[SystemNodes] ADD CONSTRAINT [FK_SystemNodes_SystemNodes] 
    FOREIGN KEY ([ParentNodeID]) REFERENCES [dbo].[SystemNodes] ([NodeID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[SystemNodes] ADD CONSTRAINT [FK_SystemNodes_Permissions] 
    FOREIGN KEY ([PermissionCode]) REFERENCES [dbo].[Permissions] ([PermissionCode]) ON DELETE NO ACTION ON UPDATE CASCADE;
GO

ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Roles] 
    FOREIGN KEY ([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Permissions] 
    FOREIGN KEY ([PermissionCode]) REFERENCES [dbo].[Permissions] ([PermissionCode]) ON DELETE CASCADE;
GO

ALTER TABLE [dbo].[Accounts] ADD CONSTRAINT [FK_Accounts_Employee] 
    FOREIGN KEY ([EmployeeID]) REFERENCES [dbo].[Employee] ([EmployeeID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Accounts] 
    FOREIGN KEY ([AccountID]) REFERENCES [dbo].[Accounts] ([AccountID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Roles] 
    FOREIGN KEY ([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE NO ACTION;
GO

-- 前台選單載入優化索引 (過濾掉按鈕)
CREATE NONCLUSTERED INDEX [IX_SystemNodes_NavMenuEngine]
ON [dbo].[SystemNodes] ([ParentNodeID] ASC, [SortSeq] ASC)
INCLUDE ([NodeID], [NodeName], [FormClassPath], [PermissionCode])
WHERE [NodeType] IN (1, 2) AND [IsActive] = 1;
GO

CREATE NONCLUSTERED INDEX [IX_Roles_StatusLookup]
ON [dbo].[Roles] ([IsActive] ASC, [IsSystem] ASC)
INCLUDE ([RoleCode], [RoleName]);
GO

CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionID_Nav] 
ON [dbo].[RolePermissions] ([PermissionCode] ASC, [RoleID] ASC);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Accounts_Username_Filtered]
ON [dbo].[Accounts] ([Username]) WHERE [IsActive] = 1;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Accounts_EmployeeID_Filtered]
ON [dbo].[Accounts] ([EmployeeID]) WHERE [IsActive] = 1;
GO

CREATE NONCLUSTERED INDEX [IX_Accounts_Login_Validation]
ON [dbo].[Accounts] ([Username] ASC, [IsActive] ASC)
INCLUDE ([PasswordHash], [IsLocked], [FailedCount]);
GO

CREATE NONCLUSTERED INDEX [IX_UserRoles_ReverseLookup]
ON [dbo].[UserRoles] ([RoleID] ASC, [AccountID] ASC);
GO

-- ========================================================
-- [基本檔模組] 外鍵與索引
-- ========================================================
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [FK_Customer_Base_District] 
    FOREIGN KEY ([DistrictID]) REFERENCES [dbo].[Base_District] ([DistrictID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[Vendor] ADD CONSTRAINT [FK_Vendor_District] 
    FOREIGN KEY ([DistrictID]) REFERENCES [dbo].[Base_District] ([DistrictID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[Employee] ADD CONSTRAINT [FK_Employee_Base_District] 
    FOREIGN KEY ([DistrictID]) REFERENCES [dbo].[Base_District] ([DistrictID]) ON DELETE NO ACTION;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Customer_Email_Filtered]
ON [dbo].[Customer]([Email]) WHERE [Email] IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX [UX_Customer_TaxID_Filtered]
ON [dbo].[Customer] ([TaxID] ASC) WHERE [TaxID] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Vendor_TaxID_Filtered]
ON [dbo].[Vendor]([TaxID]) WHERE [TaxID] IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX [UQ_Employee_Email_Filtered]
ON [dbo].[Employee]([Email]) WHERE [Email] IS NOT NULL;
GO

-- ========================================================
-- [交易單據模組] 外鍵與索引
-- ========================================================
ALTER TABLE [dbo].[SalesMaster] ADD CONSTRAINT [FK_SalesMaster_Customer] 
    FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customer] ([CustomerID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[SalesMaster] ADD CONSTRAINT [FK_SalesMaster_Base_District] 
    FOREIGN KEY ([ShipDistrictID]) REFERENCES [dbo].[Base_District] ([DistrictID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[SalesDetail] ADD CONSTRAINT [FK_SalesDetail_SalesMaster] 
    FOREIGN KEY ([SalesID]) REFERENCES [dbo].[SalesMaster] ([SalesID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[SalesDetail] ADD CONSTRAINT [FK_SalesDetail_Product] 
    FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Product] ([ProductID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[SalesDetail] ADD CONSTRAINT [UQ_SalesDetail_Sales_Product] 
    UNIQUE NONCLUSTERED ([SalesID], [ProductID]);
GO

ALTER TABLE [dbo].[PurchaseMaster] ADD CONSTRAINT [FK_PurchaseMaster_Vendor] 
    FOREIGN KEY ([VendorID]) REFERENCES [dbo].[Vendor] ([VendorID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[PurchaseDetail] ADD CONSTRAINT [FK_PurchaseDetail_PurchaseMaster] 
    FOREIGN KEY ([PurchaseID]) REFERENCES [dbo].[PurchaseMaster] ([PurchaseID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[PurchaseDetail] ADD CONSTRAINT [FK_PurchaseDetail_Product] 
    FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Product] ([ProductID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[PurchaseDetail] ADD CONSTRAINT [UQ_PurchaseDetail_Purchase_Product] 
    UNIQUE NONCLUSTERED ([PurchaseID], [ProductID]);
GO

ALTER TABLE [dbo].[InventoryMaster] ADD CONSTRAINT [FK_InventoryMaster_Employee] 
    FOREIGN KEY ([EmployeeID]) REFERENCES [dbo].[Employee] ([EmployeeID]) ON DELETE NO ACTION;
GO

ALTER TABLE [dbo].[InventoryDetail] ADD CONSTRAINT [FK_InventoryDetail_InventoryMaster] 
    FOREIGN KEY ([InventoryID]) REFERENCES [dbo].[InventoryMaster] ([InventoryID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[InventoryDetail] ADD CONSTRAINT [FK_InventoryDetail_Product] 
    FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Product] ([ProductID]) ON DELETE NO ACTION;
ALTER TABLE [dbo].[InventoryDetail] ADD CONSTRAINT [UQ_InventoryDetail_Inventory_Product] 
    UNIQUE NONCLUSTERED ([InventoryID], [ProductID]);
GO

CREATE NONCLUSTERED INDEX [IX_SalesMaster_ShipDistrictID] ON [dbo].[SalesMaster] ([ShipDistrictID] ASC);
CREATE NONCLUSTERED INDEX [IX_SalesMaster_CustomerID_IsActive] ON [dbo].[SalesMaster] ([CustomerID] ASC) INCLUDE ([SalesNo], [SalesDate], [TotalAmount], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_PurchaseMaster_VendorID] ON [dbo].[PurchaseMaster] ([VendorID] ASC) INCLUDE ([PurchaseNo], [PurchaseDate], [TotalAmount], [Status]);
GO

CREATE NONCLUSTERED INDEX [IX_InventoryMaster_EmployeeID_Status] ON [dbo].[InventoryMaster] ([EmployeeID] ASC, [InventoryDate] ASC) INCLUDE ([InventoryNo], [Status]);
GO