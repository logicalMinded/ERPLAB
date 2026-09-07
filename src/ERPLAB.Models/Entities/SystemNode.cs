namespace ERPLAB.Models.Entities
{
    /// <summary>
    /// 系統節點實體 (Entity)。
    /// 映射資料庫 [SystemNodes] 資料表，負責定義系統導覽選單與權限資源之階層結構。
    /// 透過自我參考 (Self-Referencing) 建立樹狀架構，並結合類別反射 (Reflection) 與 RBAC 權限代碼，以實現動態選單與存取控制。
    /// </summary>
    public class SystemNode : ISoftDeletable
    {
        // 系統主鍵 (Primary Key)
        public int NodeID { get; set; }

        public string NodeName { get; set; } = string.Empty;

        // 節點資源類型 (1: 模組目錄, 2: 表單頁面, 3: 功能按鈕)
        public byte NodeType { get; set; }

        // 父節點 ID。採自我參考 (Self-Referencing) 外鍵設計，以建構無限層級之樹狀結構
        public int? ParentNodeID { get; set; }

        // 前端介面呈現順序之排序權重
        public int SortSeq { get; set; }

        // 前端表單之反射 (Reflection) 類別路徑，供系統動態載入與實例化 UI 元件 (僅適用於 NodeType = 2)
        public string? FormClassPath { get; set; }

        // 權限代碼。關聯至權限主檔 (Permission) 之自然主鍵 (Natural Key)，作為 RBAC 授權檢核之比對依據
        public string? PermissionCode { get; set; }

        // =====================================================================
        // 實作 ISoftDeletable 介面：邏輯刪除 (Soft Delete) 標記
        // =====================================================================
        public bool IsActive { get; set; } = true;
    }
}