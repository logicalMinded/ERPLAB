using ERPLAB.Models.Exceptions;
using System.Data;
namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 系統共用分頁基底類別 (繼承自 UserControl)。
    /// 核心職責：將系統共用之 UI 邏輯進行抽象化收斂，包含：RBAC 權限之物理隱藏、
    /// 單據狀態機之唯讀鎖定，以及非同步作業 (Async) 之例外攔截與訊息處理沙盒。
    /// </summary>
    public class BasePage : UserControl
    {
        public BasePage()
        {
            // 開啟雙重緩衝 (Double Buffering) 機制。
            // 解決 WinForms 於渲染包含大量子控制項 (如 DataGridView) 時產生之畫面閃爍問題。
            this.DoubleBuffered = true;
        }

        // =====================================================================
        // 權限檢核防護機制 (RBAC Authorization)
        // 採物理隱藏策略：不具權限之控制項將直接自畫面渲染樹中抹除或隱藏 (Visible = false)，
        // 防範惡意使用者透過記憶體修改工具將 Enabled 屬性竄改以規避限制。
        // =====================================================================

        /// <summary>
        /// 權限檢核多載一：適用於繼承自 Control 之標準控制項。
        /// </summary>
        protected void RequirePermission(string permissionCode, Control control)
        {
            if (control == null) return;

            // 透過 SessionContext 查詢快取之授權狀態，時間複雜度為 O(1)
            bool isAuthorized = SessionContext.HasPermission(permissionCode);

            // 特例處理：WinForms 之 TabPage 控制項設定 Visible 無效，必須實施物理卸載
            if (control is TabPage tabPage)
            {
                if (!isAuthorized && tabPage.Parent is TabControl parentTabControl)
                {
                    parentTabControl.TabPages.Remove(tabPage);
                }
                return;
            }

            control.Visible = isAuthorized;
        }

        /// <summary>
        /// 權限檢核多載二：適用於選單與工具列項目 (非繼承自 Control 之 ToolStripItem 家族)。
        /// </summary>
        protected void RequirePermission(string permissionCode, ToolStripItem item)
        {
            if (item == null) return;
            item.Visible = SessionContext.HasPermission(permissionCode);
        }

        /// <summary>
        /// 權限檢核多載三：適用於資料表欄位 (DataGridViewColumn)。
        /// 供隱藏機敏欄位 (如進貨成本、毛利率) 使用。
        /// </summary>
        protected void RequirePermission(string permissionCode, DataGridViewColumn column)
        {
            if (column == null) return;
            column.Visible = SessionContext.HasPermission(permissionCode);
        }

        // =====================================================================
        // 單據狀態機防護機制 (State Machine UI Locking)
        // 於單據過帳或作廢等不可逆狀態下，對 UI 實施全域唯讀鎖定，
        // 阻絕無效更新操作，減少後端資料庫層無謂的驗證與 I/O 成本。
        // =====================================================================

        /// <summary>
        /// 檢核單據狀態，若觸發鎖定條件，則發動遞迴鎖死控制項結構。
        /// </summary>
        protected void LockUIForStatus(byte currentStatus, byte lockedStatus)
        {
            if (currentStatus != lockedStatus) return;

            RecursiveLockControls(this.Controls);
        }

        /// <summary>
        /// 透過深度優先搜尋 (DFS) 遞迴鎖定控制項及其子元件。
        /// </summary>
        private void RecursiveLockControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl.HasChildren)
                {
                    RecursiveLockControls(ctrl.Controls);
                }

                // 依據控制項特性實施降級鎖定策略 (優先採用 ReadOnly 保留內容複製能力，次之為 Enabled)
                switch (ctrl)
                {
                    case TextBox txt:
                        txt.ReadOnly = true;
                        break;

                    case ComboBox cmb:
                        cmb.Enabled = false;
                        break;

                    case DateTimePicker dtp:
                        dtp.Enabled = false;
                        break;

                    case CheckBox chk:
                        chk.Enabled = false;
                        break;

                    case Button btn:
                        // 豁免機制：供關閉視窗、列印報表等非資料寫入型按鈕排除鎖定
                        if (btn.Tag?.ToString() != "IgnoreLock")
                        {
                            btn.Enabled = false;
                        }
                        break;

                    case DataGridView dgv:
                        dgv.ReadOnly = true;
                        dgv.AllowUserToAddRows = false;
                        dgv.AllowUserToDeleteRows = false;
                        break;
                }
            }
        }

        // =====================================================================
        // 驗證輔助工具 (Validation Helper)
        // 封裝通用之資料驗證回饋流程。
        // =====================================================================

        /// <summary>
        /// 處理 Tuple 格式之驗證結果，並自動處理錯誤提示與游標焦點轉移。
        /// </summary>
        protected bool EnsureValid((bool IsValid, string ErrorMsg) validationResult, Control? focusControl = null)
        {
            if (validationResult.IsValid) return true;

            MessageBox.Show(validationResult.ErrorMsg, "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focusControl?.Focus();

            return false;
        }

        // =====================================================================
        // UI 層例外攔截沙盒 (Exception Handling Sandbox)
        // 核心職責：將商業邏輯層 (BLL) 封裝為委派 (Delegate) 執行，
        // 統一捕捉樂觀鎖異常 (DBConcurrencyException) 與業務邏輯異常 (BusinessRuleException)，
        // 保持子表單程式碼純淨，並確保系統穩定性。
        // =====================================================================

        /// <summary>
        /// 提供非同步作業之安全執行沙盒。
        /// </summary>
        /// <param name="bllAction">待執行之商業邏輯作業</param>
        /// <param name="reloadDataAction">選填，發生樂觀鎖衝突時自動執行之資料重載作業</param>
        protected async Task<bool> SafeExecuteAsync(Func<Task> bllAction, Func<Task>? reloadDataAction = null)
        {
            try
            {
                await bllAction();
                return true;
            }
            catch (DBConcurrencyException cx)
            {
                MessageBox.Show(cx.Message, "資料衝突", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (reloadDataAction != null)
                {
                    await reloadDataAction();
                }

                return false;
            }
            catch (BusinessRuleException brex)
            {
                MessageBox.Show(brex.Message, "業務檢核失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"發生未預期的系統錯誤：\n{ex.Message}", "系統異常", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}