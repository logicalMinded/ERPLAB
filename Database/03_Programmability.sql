-- =====================================================================
-- 專案名稱：ERPLAB 企業資源規劃系統
-- 檔案名稱：03_Programmability.sql
-- 執行順序：3 / 4
-- 核心職責：建立所有 Views (檢視表) 與 Triggers (觸發程序)。
-- 物理目的：負責 RBAC 權限矩陣的視圖收斂，以及實體資料表的防黑手審計追蹤(主要用作避免手動寫入審計資料)。
-- =====================================================================

USE [ERPLAB2026];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ========================================================
-- 💡 嚴密的權限審查檢視表：核心就在 JOIN Roles 時釘死 [IsActive] = 1
-- 只要角色被停用，既存的 User 透過這個 View 就完全撈不到任何 PermissionCode，特權當場蒸發！
-- ========================================================
CREATE VIEW [dbo].[vw_Account_ActivePermissions]
AS
SELECT 
    a.[AccountID],
    a.[Username],
    r.[RoleCode],
    p.[PermissionCode]
FROM [dbo].[Accounts] a
INNER JOIN [dbo].[UserRoles] ur       ON a.[AccountID] = ur.[AccountID]
INNER JOIN [dbo].[Roles] r            ON ur.[RoleID] = r.[RoleID] AND r.[IsActive] = 1
INNER JOIN [dbo].[RolePermissions] rp ON r.[RoleID] = rp.[RoleID]
INNER JOIN [dbo].[Permissions] p      ON rp.[PermissionCode] = p.[PermissionCode] AND p.[IsActive] = 1
WHERE a.[IsActive] = 1;
GO

-- ========================================================
-- 建立階層關係錯亂攔截觸發器 (SystemNodes)
-- ========================================================
CREATE TRIGGER [dbo].[TR_SystemNodes_BloodlineCheck]
ON [dbo].[SystemNodes]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted WHERE [ParentNodeID] IS NOT NULL)
    BEGIN
        -- 💡 鐵血攔截：跨列反查老爸的真實 NodeType
        -- 確保 頁面(2) 的老爸一定是 模組(1)；按鈕(3) 的老爸一定是 頁面(2)
        IF EXISTS (
            SELECT 1 
            FROM inserted i
            INNER JOIN [dbo].[SystemNodes] parent ON i.[ParentNodeID] = parent.[NodeID]
            WHERE 
                (i.[NodeType] = 2 AND parent.[NodeType] <> 1) OR 
                (i.[NodeType] = 3 AND parent.[NodeType] <> 2)    
        )
        BEGIN
            RAISERROR ('【資安與結構崩潰攔截】系統節點樹狀階層錯誤：模組(1)底下只能掛頁面(2)，頁面(2)底下只能掛按鈕(3)！', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
END;
GO

-- ========================================================
-- 防黑手審計觸發程序群集 (Audit Triggers，主要用作避免手動寫入審計資料)
-- 核心防禦：防範直接下 SQL 更新時的萬用寫法，強制覆寫 DbUpdateTime 與 DbUpdateUser 與(或) DbCreateTime 與 DbCreateUser
-- ========================================================

CREATE TRIGGER [dbo].[TR_Roles_UpdateAudit] ON [dbo].[Roles] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE r SET r.[DbUpdateTime] = GETDATE(), r.[DbUpdateUser] = SUSER_SNAME()
        FROM [dbo].[Roles] r INNER JOIN inserted i ON r.[RoleID] = i.[RoleID];
    END
END;
GO

CREATE TRIGGER [dbo].[TR_Roles_InsertAudit] ON [dbo].[Roles] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE r SET r.[DbCreateTime] = GETDATE(), r.[DbCreateUser] = SUSER_SNAME(), r.[DbUpdateTime] = GETDATE(), r.[DbUpdateUser] = SUSER_SNAME()
        FROM [dbo].[Roles] r INNER JOIN inserted i ON r.[RoleID] = i.[RoleID];
    END
END;
GO

CREATE TRIGGER [dbo].[TR_RolePermissions_InsertAudit] ON [dbo].[RolePermissions] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE r SET r.[DbCreateTime] = GETDATE(), r.[DbCreateUser] = SUSER_SNAME()
        FROM [dbo].[RolePermissions] r INNER JOIN inserted i ON r.[RoleID] = i.[RoleID] AND r.[PermissionCode] = i.[PermissionCode];
    END
END;
GO

CREATE TRIGGER [dbo].[TR_Accounts_UpdateAudit] ON [dbo].[Accounts] AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE a SET a.[DbUpdateTime] = GETDATE(), a.[DbUpdateUser] = SUSER_SNAME()
        FROM [dbo].[Accounts] a INNER JOIN inserted i ON a.[AccountID] = i.[AccountID] INNER JOIN deleted d ON a.[AccountID] = d.[AccountID]
        WHERE i.[Username] <> d.[Username] OR i.[PasswordHash] <> d.[PasswordHash] OR i.[IsActive] <> d.[IsActive] OR i.[IsLocked] <> d.[IsLocked] OR i.[EmployeeID] <> d.[EmployeeID];
    END
END;
GO

CREATE TRIGGER [dbo].[TR_Accounts_InsertAudit] ON [dbo].[Accounts] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE a SET a.[DbCreateTime] = GETDATE(), a.[DbCreateUser] = SUSER_SNAME(), a.[DbUpdateTime] = GETDATE(), a.[DbUpdateUser] = SUSER_SNAME()
        FROM [dbo].[Accounts] a INNER JOIN inserted i ON a.[AccountID] = i.[AccountID];
    END
END;
GO

CREATE TRIGGER [dbo].[TR_UserRoles_InsertAudit] ON [dbo].[UserRoles] AFTER INSERT AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        UPDATE u SET u.[DbCreateTime] = GETDATE(), u.[DbCreateUser] = SUSER_SNAME()
        FROM [dbo].[UserRoles] u INNER JOIN inserted i ON u.[AccountID] = i.[AccountID] AND u.[RoleID] = i.[RoleID];
    END
END;
GO

-- ========================================================
-- 狀態機死鎖防禦觸發程序 (State Lock-down Trigger)
-- ========================================================
CREATE TRIGGER [dbo].[TR_InventoryMaster_ProtectDelete]
ON [dbo].[InventoryMaster]
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 核心審查：檢查準備被物理刪除的單據中，是否夾帶任何「已確認過帳 (Status = 2)」的致命資料
    IF EXISTS (SELECT 1 FROM deleted WHERE [Status] = 2)
    BEGIN
        -- 拋出資安與內控高壓警告， Error 級別 16 會直接中斷後端 C# 執行流程
        RAISERROR (N'【內控嚴重警告】選取的盤點單據已完成審核過帳程序，於系統運行期絕對禁止物理刪除！', 16, 1);
        ROLLBACK TRANSACTION; 
        RETURN;
    END

    -- 防線通過：執行真正的物理刪除，釋放磁碟空間
    DELETE FROM [dbo].[InventoryMaster] WHERE [InventoryID] IN (SELECT [InventoryID] FROM deleted);
END;
GO