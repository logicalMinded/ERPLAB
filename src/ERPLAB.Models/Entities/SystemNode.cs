namespace ERPLAB.Models.Entities
{
    public class SystemNode : ISoftDeletable
    {
        public int NodeID { get; set; }

        // 節點呈現名稱 (NVARCHAR 50)
        public string NodeName { get; set; } = string.Empty;

        // 節點資源類型 (1:模組, 2:頁面, 3:按鈕)，對應 TINYINT
        public byte NodeType { get; set; }

        // 自我引用外鍵，定義樹狀結構
        public int? ParentNodeID { get; set; }

        // UI 排序權重
        public int SortSeq { get; set; }

        // C# WinForms 視窗實體反射路徑 (僅 NodeType=2 適用)
        public string? FormClassPath { get; set; }

        // 關聯扁平化權限字典的自然鍵
        public string? PermissionCode { get; set; }

        // --- 實作 ISoftDeletable 介面 ---
        public bool IsActive { get; set; } = true;
    }
}