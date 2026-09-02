# ERPLAB 資料庫實體與防禦架構

本目錄包含 ERPLAB 系統的 SQL Server 部署腳本。本專案採用純 T-SQL 構建資料庫實體，未依賴 ORM 的自動建表功能，以確保對資料完整性、併發控制與 I/O 效能的精確掌握。

## 1. 實體關聯圖 (ER Models)
- [核心資安與 RBAC 權限模型](../docs/diagrams/er_01_security_rbac.png)
- [進銷存核心與主明細交易模型](../docs/diagrams/er_02_trading_inventory.png)
- [基礎主檔與地理連動模型](../docs/diagrams/er_03_master_geography.png)

## 2. 資料完整性與內控設計 (Data Integrity & Control)

### 樂觀鎖併發控制 (Optimistic Concurrency)
- **實作方式**：於高頻異動主檔掛載 `[RowVersion] TIMESTAMP` 欄位。
- **目的**：配合 ADO.NET 寫入時的版本比對，在不升級交易隔離層級的前提下，防止多使用者同時編輯造成的「遺失更新 (Lost Update)」。

### 審計軌跡 (Audit Trail Triggers)
- **實作方式**：佈署 `AFTER UPDATE` 觸發程序。
- **目的**：強制覆寫 `DbUpdateTime` 與 `DbUpdateUser` (透過 `SUSER_SNAME()`)。確保即使透過後台工具直接異動資料，仍能保留具備不可否認性的作業系統連線足跡。

### 狀態機防禦 (State Machine Triggers)
- **實作方式**：於單據表佈署 `INSTEAD OF DELETE` 觸發程序。
- **目的**：單據過帳後 (`Status = 2`)，由資料庫引擎層級攔截並拒絕 DELETE 指令，確保財務與庫存歷史資料的不可變性 (Immutability)。

## 3. 查詢與寫入優化 (Performance Tuning)

### 表值參數批次寫入 (Table-Valued Parameters, TVP)
- **實作方式**：建立如 `[dbo].[SalesDetailType]` 等使用者自訂資料表型別 (UDTT)。
- **目的**：將 C# 端的明細集合轉為 `DataTable` 單次傳送。將數百筆明細的 `INSERT` 動作壓縮為單次網路往返 (1 Round-Trip)，降低連線池負載。

### 過濾唯一索引 (Filtered Unique Index)
- **實作方式**：建立 `CREATE UNIQUE NONCLUSTERED INDEX ... WHERE [IsActive] = 1`。
- **目的**：在系統採用軟刪除 (Soft Delete) 保留歷史紀錄的架構下，依然能利用資料庫索引，確保「啟用中」的業務鍵 (如：統編、帳號) 不發生重複。

## 4. 系統初始化與測試資料 (System Initialization & Mock Data)

`04_SeedData.sql` 腳本包含系統運作所需的基礎設定值與測試資料，提供建置後可直接進行系統功能驗證的預設環境：

### 核心字典與取號設定 (Core Infrastructure)
- **內容**：包含台灣縣市與行政區代碼，以及各單據的自動取號字首設定（`AutoNumber`）。
- **目的**：建立資料庫外部鍵（FK）的參照基礎，並提供前端 UI 縣市區域連動及單據自動取號功能的必要設定。

### 權限配置與測試帳號 (Security Configuration)
- **內容**：包含權限代碼、角色設定、系統選單節點（`SystemNodes`），以及預設的管理員與一般測試帳號。
- **目的**：預載相容 ASP.NET Core Identity V3 格式的 PBKDF2 密碼雜湊，供登入驗證使用；並建立基礎權限關聯，以利驗證反射動態選單與介面權限控制邏輯。

### 業務測試資料 (Business Mock Data)
- **內容**：建立包含企業與個人類型之客戶、廠商、員工及商品等基本檔測試資料。
- **目的**：提供包含 `NULL` 值、不同格式長度（如 3 碼與 6 碼郵遞區號）與多種狀態碼的測試集，用於驗證前端輸入檢核、空值處理機制，以及資料庫 OFFSET-FETCH 分頁查詢的正確性。
