using System.ComponentModel;

namespace ERPLAB.Models.Enums
{
    // =====================================================================
    // 1. 單據業務狀態機 (Document Status)
    // 適用資料表：SalesMaster, PurchaseMaster (對齊中大型標準 4 狀態模型)
    // =====================================================================
    public enum DocumentStatus : byte
    {
        [Description("未過帳")] Draft = 1,          // 草稿，可自由增刪改
        [Description("已過帳")] Posted = 2,         // 正式單據，UI 鎖死唯讀，牽動庫存/帳款
        [Description("已註銷")] Cancelled = 3,      // 未過帳前被取消的單據 (無財務影響)
        [Description("已作廢")] Voided = 4          // 已過帳後被反轉的單據 (具備財務沖銷紀錄)
    }

    // =====================================================================
    // 2. 系統節點類型 (System Node Type)
    // 適用資料表：SystemNodes
    // =====================================================================
    public enum SystemNodeType : byte
    {
        [Description("模組")] Module = 1,         // 根節點 (如：基本資料管理、進銷存作業)
        [Description("作業頁面")] Page = 2,           // 實體作業畫面 (如：廠商資料維護)
        [Description("操作按鈕")] Action = 3          // 畫面內的機敏功能 (如：新增、作廢)
    }

    // =====================================================================
    // 3. 員工在職狀態 (Employee Job Status)
    // 適用資料表：Employee (與 IsActive 系統權限發動物理連動)
    // =====================================================================
    public enum EmployeeJobStatus : byte
    {
        [Description("離職")] Resigned = 0,       // 實體離開，強制觸發 IsActive = 0
        [Description("留職停薪")] UnpaidLeave = 1,    // 暫停職務，強制觸發 IsActive = 0
        [Description("在職")] Active = 2          // 正常任職，觸發 IsActive = 1
    }

    // =====================================================================
    // 4. 性別代碼 (Gender)
    // 適用資料表：Customer, Employee, Vendor
    // =====================================================================
    public enum GenderType : byte
    {
        [Description("其他")] Other = 0,
        [Description("男")] Male = 1,
        [Description("女")] Female = 2
    }
}