# ERPLAB 企業進銷存管理系統

![C#](https://img.shields.io/badge/C%23-9.0%2B-blue) ![.NET](https://img.shields.io/badge/.NET-8.0-purple) ![SQL Server](https://img.shields.io/badge/SQL_Server-2022-red) ![WinForms](https://img.shields.io/badge/WinForms-Desktop-lightgrey)

本專案為針對企業內部進銷存作業開發之桌面端 ERP 系統。系統採用三層式架構 (3-Tier Architecture) 進行模組化開發，並以 ADO.NET 作為底層資料存取技術，旨在提供穩定的資料庫 I/O 效能、多使用者併發控制，以及嚴謹的資料完整性檢核。

---

## 🚀 快速啟動指南 (Quick Start)

### 1. 環境準備
*   IDE: Visual Studio 2022 (支援 .NET 8.0)
*   Database: Microsoft SQL Server 2022 (Developer / Express 均可)

### 2. 資料庫建置
請透過 SSMS 依序執行 `Database/` 目錄下的 SQL 腳本：
1.  `01_Schema.sql`：建立資料表與 UDTT (使用者自訂資料表型別)。
2.  `02_Constraints.sql`：建立外部鍵 (FK)、CHECK 約束與過濾唯一索引。
3.  `03_Programmability.sql`：建立觸發程序 (Triggers) 與檢視表。
4.  `04_SeedData.sql`：寫入地理字典、RBAC 權限矩陣與系統預設帳號。

### 3. 系統登入憑證
編譯並啟動 `ERPLAB.UI` 專案後，請使用以下預設帳號登入進行測試：
*   ADMIN： 帳號 [ `user26006` / `user26007` ] / 密碼 [ `1234` ]
*   DVANCED_USER： 帳號 [ `user26001` / `user26010` ] / 密碼 [ `1234` ]
*   GENERAL_USER： 帳號 [ `user26009` ] / 密碼 [ `1234` ]

---

## 🏛️ 系統架構 (System Architecture)

本系統嚴格落實專案參考隔離，確保各層職責單一化：

```mermaid
graph TD
    UI[ERPLAB.UI <br> 表現層] -->|Method Call| BLL
    BLL[ERPLAB.BLL <br> 商業邏輯層] -->|Delegate Execution| DAL
    DAL[ERPLAB.DataAccess <br> 資料存取層] -->|TDS Protocol| DB[(SQL Server)]
    
    UI -.-> Models[ERPLAB.Models <br> 領域模型與 DTO]
    BLL -.-> Models
    DAL -.-> Models
```

*   **ERPLAB.UI**：負責視窗生命週期、狀態機控制與資料雙向綁定。不具備資料庫存取套件參考。
*   **ERPLAB.BLL**：負責跨模組商業規則運算、單號配發機制與狀態流轉檢核。
*   **ERPLAB.DataAccess**：負責 SQL 參數化封裝、批次寫入與非同步資料庫 I/O。

---

## 🗄️ 資料庫實體模型 (ER Models)

系統資料庫依據業務領域劃分為三大核心模型。詳細圖解請參閱 `docs/diagrams/` 目錄：

1.  **[RBAC 權限與資安模型](./docs/diagrams/er_01_security_rbac.png)**：
    採多對多關聯設計。將 `Accounts` 與 `Employee` 實體分離，並透過 `SystemNodes` 與 `Permissions` 表建立動態選單路由機制。
	<details>
	<summary><b>點擊展開 ER 圖</b></summary>

	```mermaid
	erDiagram
    %% =========================================================
    %% 關聯定義 (嚴守 IE Notation)
    %% =========================================================
    Employee ||..o| Accounts : "(ON DELETE NO ACTION)"
    Accounts ||--o{ UserRoles : "(ON DELETE NO ACTION)"
    Roles ||--o{ UserRoles : "(ON DELETE NO ACTION)"
    Roles ||--o{ RolePermissions : "(ON DELETE NO ACTION)"
    Permissions ||--o{ RolePermissions : "(ON DELETE CASCADE)"
    Permissions |o..o{ SystemNodes : "(ON DELETE NO ACTION)     (ON UPDATE CASCADE)"
    SystemNodes |o..o{ SystemNodes : "(ON DELETE NO ACTION)"
    %% =========================================================
    %% 實體定義
    %% =========================================================
    Accounts {
        AccountID int PK
        EmployeeID int FK 
        Username varchar(50) UK "過濾唯一索引 (UK)"
        PasswordHash varchar(255) "Identity V3 封裝"
        IsLocked bit "防暴力破解鎖定"
        FailedCount tinyint
        IsActive bit "軟刪除防線"
        RowVersion timestamp "樂觀鎖"
        other 其它欄位
    }
    Roles {
        RoleID int PK
        RoleCode varchar(50) UK
        IsSystem bit "系統內建防護"
        IsActive bit "暫時隔離用"
        other 其它欄位
    }
    Permissions {
        PermissionCode varchar(100) PK "自然主鍵"
        PermissionName nvarchar(50) 
        IsActive bit "暫時隔離用"
    }
    UserRoles {
        AccountID int PK, FK "複合主鍵"
        RoleID int PK, FK "複合主鍵"
        other 其它欄位
    }
    RolePermissions {
        RoleID int PK, FK "複合主鍵"
        PermissionCode varchar(100) PK, FK "複合主鍵"
        other 其它欄位
    }
    SystemNodes {
        NodeID int PK
        NodeType tinyint
        ParentNodeID int FK "自我參照 (樹狀結構)" 
        FormClassPath varchar(255) "UI 反射路由字串"
        PermissionCode varchar(100) FK "允許 NULL (目錄節點)"
        IsActive bit "暫時隔離用"
        other 其它欄位
    }
    Employee {
        EmployeeID int PK
        EmployeeNo varchar(20) UK
        JobStatus tinyint "人事狀態"
        IsActive bit "軟刪除防線" 
        RowVersion timestamp "樂觀鎖"       
        other 其它欄位
    }
    ```

	</details>
2.  **[進銷存主明細交易模型](./docs/diagrams/er_02_trading_inventory.png)**：
    實作 Master-Detail 架構。單據過帳後受 `INSTEAD OF DELETE` 觸發程序保護；庫存異動採用差異沖平演算法確保帳實一致。
	<details>
	<summary><b>點擊展開 ER 圖</b></summary>

	```mermaid
	erDiagram
    %% =========================================================
    %% 關聯定義 (全數為非識別性弱關聯)
    %% =========================================================
    Customer ||..o{ SalesMaster : "(ON DELETE NO ACTION)"
    Base_District ||..o{ SalesMaster : "(ON DELETE NO ACTION)"
    SalesMaster ||..|{ SalesDetail : "(ON DELETE NO ACTION)"
    Product ||..o{ SalesDetail : "(ON DELETE NO ACTION)"
    %% =========================================================
    %% 實體定義 (AutoNumber 無實體 FK，故獨立展示)
    %% =========================================================
    Customer {
        CustomerID int PK
        CustomerNo varchar(20) UK "單據號碼 (來自 AutoNumber)"
        CustomerName nvarchar(50)
        IsActive bit "軟刪除防線"
        RowVersion timestamp "樂觀鎖"
        other 其它欄位
    }
    SalesMaster {
        SalesID bigint PK
        SalesNo varchar(20) UK "單據號碼 (來自 AutoNumber)"
        CustomerID int FK
        ShipDistrictID int FK
        ShipZipCode varchar(6) "歷史郵遞區號(快照)"
        ShipAddress nvarchar(200) "歷史出貨地址(快照)"
        TotalAmount decimal(18,2) "總計 (反正規化快取)"
        Status tinyint "4維狀態機: 1=草稿, 2=過帳..."
        RowVersion timestamp "樂觀鎖"
        other 其它欄位
    }
    SalesDetail {
        SalesDID bigint PK "非叢集主鍵"
        SalesID bigint FK "複合叢集索引 (與 LineNo 綁定)"
        LineNo int "明細行號"
        ProductID int FK "關聯商品"
        UnitPrice decimal(18,2) "歷史單價 (快照)"
        UnitCost decimal(18,2) "歷史成本快照"
        Qty int 
        other 其它欄位
    }
    Product {
        ProductID int PK
        ProductNo varchar(20) UK "單據號碼 (來自 AutoNumber)"
        ProductName nvarchar(100) 
        CurrentStock int "實體庫存 (受 CHECK 約束)"
        IsActive bit "軟刪除防線"
        RowVersion timestamp "樂觀鎖"
        MovingAverageCost decimal "移動加權平均成本"
        other 其它欄位
    }
    AutoNumber {
		    DocType varchar PK "單據類型 (獨立微交易取號)"
        CurrentDate  DATE "當前日期 (滾動重置)"
        LastSeq int "最後流水號"
    }
	```

	</details>
3.  **[基礎主檔與地理連動模型](./docs/diagrams/er_03_master_geography.png)**：
    實作 `IsActive` 軟刪除機制，並與 `Base_City`、`Base_District` 建立實體外鍵約束，維持歷史單據的參照完整性。
	<details>
	<summary><b>點擊展開 ER 圖</b></summary>

	```mermaid
	erDiagram
    %% =========================================================
    %% 關聯定義 (全數為非識別性弱關聯)
    %% =========================================================
    Base_City ||..o{ Base_District : "ON DELETE NO ACTION"
    Base_District ||..o{ Customer : "ON DELETE NO ACTION"
    Base_District ||..o{ Vendor : "ON DELETE NO ACTION"
    Base_District ||..o{ Employee : "ON DELETE NO ACTION"
    %% =========================================================
    %% 實體定義
    %% =========================================================
    Base_City {
        CityID int PK
        CityNo varchar(10) UK "政府官方代碼"
        CityName nvarchar(20) 
        SortSeq int "UI 渲染權重"
        IsActive bit "軟刪除防線"
    }
    Base_District {
        DistrictID int PK
        CityID int FK
        ZipCode varchar(3) "精確 3 碼"
        DistrictName nvarchar(20) 
        SortSeq int "UI 渲染權重"
        IsActive bit "軟刪除防線"
    }
    Customer {
        CustomerID int PK
        CustomerNo varchar(20) UK
        DistrictID int FK "地理強外鍵"
        CustomZipCode varchar(6) "3+3 碼後綴"
        Address nvarchar(200) "街道門牌"
        other 其它欄位 
    }
    Vendor {
        VendorID int PK
        VendorNo varchar(20) UK
        DistrictID int FK "地理強外鍵"
        CustomZipCode varchar(6) "3+3 碼後綴"
        Address nvarchar(200) "街道門牌"
        other 其它欄位 
    }
    Employee {
        EmployeeID int PK
        EmployeeNo varchar(20) UK
        DistrictID int FK "地理強外鍵"
        CustomZipCode varchar(6) "3+3 碼後綴"
        Address nvarchar(200) "街道門牌"
        other 其它欄位
    }
	```

	</details>

---

## ⚙️ 核心工程實作 (Technical Implementations)

### 1. 批次寫入與取號機制最佳化 (Batch Processing & Micro-Transaction)
*   **考量**：逐筆寫入單據明細易造成連線池佔用與網路通訊延遲；而傳統取號邏輯在高併發時易引發資料庫鎖定競爭 (Lock Contention)。
*   **實作**：建立使用者自訂資料表型別 `UDTT`，透過表值參數 `TVP, Table-Valued Parameters` 將明細清單轉為 `DataTable` 執行單次批次寫入。另將「單號生成」邏輯抽離為獨立的微交易，配合 `UPDLOCK` 處理取號。
*   **效益**：大幅降低資料庫往返通訊次數 (Round-trips)，減少高頻寫入時的鎖定等待時間，提升整體 I/O 處理效率；並透過 `SqlTransaction` 確保主檔與明細寫入的資料一致性。

### 2. 樂觀鎖併發控制 (Optimistic Concurrency)
*   **考量**：多位使用者並行編輯同一單據時，易產生「遺失更新 (Lost Update)」或庫存數據不一致的風險。
*   **實作**：於業務主檔配置 `[RowVersion] TIMESTAMP` 欄位實作樂觀鎖 `Optimistic Locking`。資料存取層執行 UPDATE 時，透過 `ExecuteScalarAsync` 搭配 `OUTPUT INSERTED` 比對時間戳記，若受影響資料列為 0 則拋出 DBConcurrencyException。
*   **效益**：在不提升資料庫交易隔離層級的前提下，有效攔截併發衝突。發生衝突時引導前端重新載入最新狀態，確保資料寫入與庫存異動的一致性。

### 3. 記憶體管理與密碼學實作 (Memory-Optimized Cryptography)
*   **考量**：頻繁的密碼雜湊運算易產生大量短生命週期的字串與陣列物件，增加 GC (Garbage Collection) 負擔；常規字串比對亦存在時序攻擊 (Timing Attack) 的安全疑慮。
*   **實作**：密碼驗證模組採用 `PBKDF2` 演算法，並相容 `Identity V3` 的 `Big-Endian` 二進位封裝格式。處理過程中採用 `stackalloc` 與 `Span<byte>` 進行記憶體操作。
*   **效益**：避免頻繁生成短生命週期的 `byte[]` 陣列，降低 Garbage Collection (GC) 觸發頻率；採用 `CryptographicOperations.FixedTimeEquals` 進行常數時間比對，提升防禦時序攻擊的安全性。

### 4. 資料綁定與 UI 狀態機 (Data Binding & State Machine)
*   **考量**：傳統 WinForms 的頻繁資料更新易引發事件連鎖觸發 (Event Cascading)、畫面閃爍，以及焦點狀態不同步。
*   **實作**：擴充 `BindingList<T>` 自訂 ExtendedBindingList，實作 `AddRange` 暫停事件觸發機制。透過 `BindingSource` 統一管理資料游標，並於 `CellEndEdit` 事件進行記憶體內計算與開窗查詢。設計 `BasePage` 基底類別，依據單據狀態 (草稿/過帳/註銷/作廢) 動態控制介面元件的 `ReadOnly` 與 `Enabled` 屬性。
*   **效益**：減少 DataGridView 大量載入時的重繪次數，提升鍵盤連續輸入作業的流暢度。統一控管表單狀態，避免使用者在錯誤的單據生命週期下執行存檔或修改操作。

---

## 📸 介面展示 (Screenshots)

<details>
<summary><b>📊 銷售戰情儀表板 (BI Dashboard)</b> ── <i>點擊展開檢視</i></summary>

展示基於 SQL 聚合函數的營運數據與排行，提供管理層檢視即時營運指標。

![戰情儀表板](./docs/screenshots/ui_sales_dashboard.png)

</details>

<details>
<summary><b>⌨️ 單據主明細作業畫面 (Master-Detail Operations)</b> ── <i>點擊展開檢視</i></summary>

以銷貨單為例，展示資料雙向綁定與鍵盤輸入之試算連動，並實作單據狀態機的 UI 唯讀鎖定。

![銷貨單實機展示](./docs/screenshots/ui_sales_order_blind_typing.webp)

</details>

<details>
<summary><b>📦 庫存盤點作業 (Inventory Check)</b> ── <i>點擊展開檢視</i></summary>

展示雙軌庫存比對機制（帳面與實盤數量）。實作盤點單的狀態流轉，並於過帳時透過差異沖平邏輯自動調整庫存，確保帳實一致。

![庫存盤點作業](./docs/screenshots/ui_inventory_check.png)

</details>

<details>
<summary><b>📇 基礎資料維護 (Master Data Management)</b> ── <i>點擊展開檢視</i></summary>

以客戶基本檔為例，展示地理資訊二級連動、自訂元件狀態更新與基礎資料的軟刪除 (Soft Delete) 實作。

![客戶基本檔](./docs/screenshots/ui_customer_crud.png)

</details>

<details>
<summary><b>🔐 動態導覽選單與權限控制 (Dynamic Menu & RBAC)</b> ── <i>點擊展開檢視</i></summary>

結合 FlowLayoutPanel 實作摺疊式導覽介面。系統會依據登入者之權限矩陣動態生成選單節點，並透過反射 (Reflection) 機制動態載入對應的業務模組實體。

![動態選單與權限](./docs/screenshots/ui_rbac_dynamic_menu.png)

</details>