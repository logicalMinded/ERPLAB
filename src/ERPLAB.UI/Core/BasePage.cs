using ERPLAB.Models.Exceptions;
using System.Data;

namespace ERPLAB.UI.Core
{
    /// <summary>
    /// 系統業務分頁基底 (繼承自 UserControl)。
    /// 核心職責：收斂所有子頁面共用的「RBAC 權限物理斷路」與「單據狀態機唯讀鎖定」邏輯。
    /// </summary>
    public class BasePage : UserControl
    {
        public BasePage()
        {
            // 💡 [效能最佳化] 開啟雙重緩衝 (Double Buffering)
            // 解決 WinForms 在繪製包含大量控制項 (如 DataGridView) 的分頁時，畫面產生嚴重閃爍的物理缺陷。
            this.DoubleBuffered = true;
        }

        // =====================================================================
        // 🛡️ [防禦引擎 A] RBAC 權限物理斷路 (Physical Circuit Breaker)
        // 核心理念：絕不信任前端記憶體狀態。無權限的元件直接從渲染樹物理隱藏，
        // 防範駭客利用記憶體修改工具 (如 Cheat Engine) 將 Enabled 屬性強行竄改為 true。
        // =====================================================================

        /// <summary>
        /// 權限檢核多載 1：支援一般標準控制項 (Button, Panel, CheckBox 等)
        /// </summary>
        protected void RequirePermission(string permissionCode, Control control)
        {
            if (control == null) return;

            // O(1) 極速查核全域記憶體中的授權 Hash 表
            bool isAuthorized = SessionContext.HasPermission(permissionCode);

            // 🚨 [特例攔截] 處理 WinForms 底層 SysTabControl32 的歷史包袱
            // TabPage 雖然繼承自 Control，但設定 Visible = false 在畫面上完全無效。
            if (control is TabPage tabPage)
            {
                if (!isAuthorized && tabPage.Parent is TabControl parentTabControl)
                {
                    // 必須採取物理抹除，將其從父容器的實體集合中強制卸載
                    parentTabControl.TabPages.Remove(tabPage);
                }
                return;
            }

            // 常規控制項的物理隱藏
            control.Visible = isAuthorized;
        }

        /// <summary>
        /// 權限檢核多載 2：支援選單與工具列 (ToolStripItem 架構)
        /// 因 ToolStripButton 等元件在底層並不繼承自 Control，必須獨立開闢多載通道。
        /// </summary>
        protected void RequirePermission(string permissionCode, ToolStripItem item)
        {
            if (item == null) return;
            item.Visible = SessionContext.HasPermission(permissionCode);
        }

        /// <summary>
        /// 權限檢核多載 3：支援資料表行 (DataGridViewColumn)
        /// 專門對付「進貨成本」或「高階主管核決數字」等機敏欄位的物理隱藏。
        /// </summary>
        protected void RequirePermission(string permissionCode, DataGridViewColumn column)
        {
            if (column == null) return;
            column.Visible = SessionContext.HasPermission(permissionCode);
        }

        // =====================================================================
        // 🛡️ [防禦引擎 B] 單據狀態機唯讀鎖定 (State Machine UI Locking)
        // 核心理念：當單據進入不可逆狀態 (如 Status = 2 已過帳)，前端必須即時物理鎖死，
        // 防止使用者修改資料後送出，導致後端 DAL 拋出例外，降低伺服器無謂的 I/O 與運算負擔。
        // =====================================================================

        /// <summary>
        /// 檢查單據目前狀態，若符合鎖定條件，則發動全表單遞迴鎖死機制
        /// </summary>
        /// <param name="currentStatus">單據目前的真實狀態碼</param>
        /// <param name="lockedStatus">觸發鎖定的門檻狀態碼 (如：2)</param>
        protected void LockUIForStatus(byte currentStatus, byte lockedStatus)
        {
            if (currentStatus != lockedStatus) return;

            // 啟動遞迴遍歷
            RecursiveLockControls(this.Controls);
        }

        /// <summary>
        /// 深度優先搜尋 (DFS) 遞迴鎖定控制項樹狀結構
        /// </summary>
        private void RecursiveLockControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                // 若為容器元件 (如 Panel, GroupBox)，必須往下遞迴鑽入
                if (ctrl.HasChildren)
                {
                    RecursiveLockControls(ctrl.Controls);
                }

                // 💡 依據控制項物理特性實施最佳鎖定策略
                // 優先使用 ReadOnly (可反白複製內容但不准改)，若無該屬性才退而求其次使用 Enabled = false
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
                        // 彈性豁免機制：若按鈕的 Tag 屬性標示為 "IgnoreLock" (例如：離開視窗、列印報表按鈕)，則不予鎖死
                        if (btn.Tag?.ToString() != "IgnoreLock")
                        {
                            btn.Enabled = false;
                        }
                        break;

                    case DataGridView dgv:
                        // 徹底封殺 DataGridView 的所有寫入途徑
                        dgv.ReadOnly = true;
                        dgv.AllowUserToAddRows = false;
                        dgv.AllowUserToDeleteRows = false;
                        break;
                }
            }
        }

        /// <summary>
        /// 💡 配合 SystemValidator Tuple 的神級防衛方法
        /// </summary>
        protected bool EnsureValid((bool IsValid, string ErrorMsg) validationResult, Control focusControl = null)
        {
            // 如果驗證通過，安全放行
            if (validationResult.IsValid) return true;

            // 驗證失敗，由 UI 專屬的 BasePage 負責彈出 MessageBox 與搶焦點！
            MessageBox.Show(validationResult.ErrorMsg, "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            focusControl?.Focus();

            return false;
        }

        // =====================================================================
        // 🛡️ [UI 交易安全沙盒] (三層式架構純淨版)
        // 核心職責：只負責攔截「商業邏輯異常」與「樂觀鎖」，徹底與 SQL 脫鉤！
        // 透過委派 (Func) 達成控制反轉 (IoC)，大幅淨化子表單程式碼。
        // =====================================================================
        protected async Task<bool> SafeExecuteAsync(Func<Task> bllAction, Func<Task>? reloadDataAction = null)
        {
            try
            {
                // 執行外部傳進來的 BLL 商業邏輯操作
                await bllAction();

                return true;
            }
            catch (DBConcurrencyException cx)
            {
                // 💡 樂觀鎖衝突攔截：提示使用者，並自動發動子表單傳入的重載資料邏輯
                MessageBox.Show(cx.Message, "資料衝突", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (reloadDataAction != null)
                {
                    await reloadDataAction();
                }

                return false;
            }
            catch (BusinessRuleException brex)
            {
                // 💡 商業邏輯攔截：接住 BLL 翻譯好的客製化業務錯誤
                // 只顯示 BLL 給的字串，達成與 SQL Server 的徹底解耦
                MessageBox.Show(brex.Message, "業務檢核失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
            catch (Exception ex)
            {
                // 🚨 系統崩潰防禦：攔截所有未預期異常 (如網路斷線、BLL 內部未處理的 Exception)
                MessageBox.Show($"發生未預期的系統錯誤：\n{ex.Message}", "系統崩潰防禦", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }
    }
}