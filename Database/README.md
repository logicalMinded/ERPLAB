# ERPLAB 資料庫實體與防禦架構

本目錄包含 ERPLAB 系統的 SQL Server 部署腳本。本專案採用純 T-SQL 構建資料庫實體，未依賴 ORM 的自動建表功能，以確保對資料完整性、併發控制與 I/O 效能的精確掌握。

## 1. 實體關聯圖 (ER Models)
- [核心資安與 RBAC 權限模型](./docs/screenshots/ui_sales_dashboard.png)
- [進銷存核心與主明細交易模型](../../docs/diagrams/er_02_trading_inventory.png)
- [基礎主檔與地理連動模型](../../docs/diagrams/er_03_master_geography.png)

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

## 4. 系統初始化與高保真測試數據 (System Initialization & Mock Data)

`04_SeedData.sql` 腳本負責建置系統首次運行的必備環境，並注入經過設計的業務測試資料，確保專案具備「零配置即開即用（Zero-Config Ready-to-use）」的端到端（E2E）驗證能力：

### 核心字典與取號引擎 (Core Infrastructure)
- **內容**：包含台灣縣市/行政區字典、以及各單據模組的自動取號字首 (`AutoNumber`)。
- **目的**：滿足資料庫層級的實體外鍵 (FK) 參照完整性，提供前端 UI 地理二級連動與微交易取號引擎的初始運作依據。

### RBAC 矩陣與資安憑證 (Security Configuration)
- **內容**：包含權限代碼、角色主檔、系統路由節點 (`SystemNodes`)，以及系統管理員與測試帳號。
- **目的**：帳號密碼欄位直接寫入相容於 Identity V3 規範的 PBKDF2 雜湊字串，確保系統部署後能順利完成記憶體切片與迭代運算，驗證動態選單反射與權限物理斷路機制。

### 高保真業務測試數據 (High-Fidelity Mock Data)
- **內容**：注入包含 B2B/B2C 混合情境的客戶、廠商、員工與商品實體資料。
- **目的**：刻意涵蓋 `NULL` 欄位、不同長度的郵遞區號與各式狀態碼，專供驗證 C# 端的「前端格式防呆」、「OFFSET-FETCH 效能分頁」與「空值安全轉換 (Null-Safety)」，提供最真實的 UI 互動體驗。