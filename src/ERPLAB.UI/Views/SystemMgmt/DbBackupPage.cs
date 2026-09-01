using ERPLAB.UI.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.UI.Views.SystemMgmt
{
    /// <summary>
    /// 資料庫備份與還原維護模組。
    /// 核心展示：動態連線組裝、T-SQL 熱備份引擎、SINGLE_USER 暴力踢人還原、終端機日誌渲染。
    /// </summary>
    public partial class DbBackupPage : BasePage
    {
        private const string DefaultBackupDirectory = @"C:\ERP_Backups";
        public DbBackupPage()
        {
            InitializeComponent();

            this.Load += DbBackupPage_Load;

            // 綁定路徑選擇事件
            btnBrowseBackupPath.Click += BtnBrowseBackupPath_Click;
            btnBrowseRestorePath.Click += BtnBrowseRestorePath_Click;

            // 綁定執行事件
            btnBackup.Click += BtnBackup_Click;
            btnRestore.Click += BtnRestore_Click;
        }

        private void DbBackupPage_Load(object? sender, EventArgs e)
        {
            // =====================================================================
            // 🛡️ [防禦引擎 A] UI 啟動瞬間發動 RBAC 物理斷路
            // 備份與還原是系統最高危險權限，必須嚴格鎖定
            // =====================================================================
            RequirePermission("ACT_DB_BACKUP", btnBackup);
            RequirePermission("ACT_DB_RESTORE", btnRestore);

            // 預設參數配置 (輔助 IT 人員快速操作)
            txtServer.Text = ".\\SQL2022"; // 預設本機
            txtDatabase.Text = "ERPLAB2026"; // 預設資料庫名稱

            AppendLog("系統就緒。請確認伺服器與資料庫名稱是否正確。");
            AppendLog("警告：還原作業將強制中斷所有線上使用者，請謹慎操作！");
        }

        // =====================================================================
        // 🖥️ [視覺引擎] 終端機風格即時日誌
        // =====================================================================
        private void AppendLog(string message)
        {
            // 確保跨執行緒呼叫安全，並自動捲動到最底端
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret(); // 物理捲動至底
        }

        // =====================================================================
        // 📁 [路徑選擇引擎]
        // =====================================================================
        private void BtnBrowseBackupPath_Click(object? sender, EventArgs e)
        {
            if (!Directory.Exists(DefaultBackupDirectory))
            {
                Directory.CreateDirectory(DefaultBackupDirectory);
            }

            using var sfd = new SaveFileDialog
            {
                Title = "選擇備份檔案儲存位置",
                Filter = "SQL Server 備份檔 (*.bak)|*.bak|所有檔案 (*.*)|*.*",
                InitialDirectory = DefaultBackupDirectory,
                FileName = $"{txtDatabase.Text.Trim()}_{DateTime.Now:yyyyMMdd_HHmm}.bak"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtBackupPath.Text = sfd.FileName;
            }
        }

        private void BtnBrowseRestorePath_Click(object? sender, EventArgs e)
        {
            if (!Directory.Exists(DefaultBackupDirectory))
            {
                Directory.CreateDirectory(DefaultBackupDirectory);
            }

            using var ofd = new OpenFileDialog
            {
                Title = "選擇要還原的備份檔案",
                Filter = "SQL Server 備份檔 (*.bak)|*.bak|所有檔案 (*.*)|*.*",
                InitialDirectory = DefaultBackupDirectory,
                CheckFileExists = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtRestorePath.Text = ofd.FileName;
            }
        }

        // =====================================================================
        // 💾 [備份引擎] T-SQL 無感熱備份 (Zero-Downtime Hot Backup)
        // =====================================================================
        private async void BtnBackup_Click(object? sender, EventArgs e)
        {
            string server = txtServer.Text.Trim();
            string database = txtDatabase.Text.Trim();
            string backupPath = txtBackupPath.Text.Trim();

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(backupPath))
            {
                MessageBox.Show("請確認伺服器、資料庫與備份路徑皆已填寫！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnBackup.Enabled = false;
            AppendLog($"開始執行資料庫 [{database}] 備份作業...");

            try
            {
                // 💡 動態組裝連線字串 (使用 Windows 整合驗證)
                string connStr = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

                // 💡 物理優化：加入 COMPRESSION 參數啟動壓縮，大幅減少硬碟 I/O 與檔案體積
                // INIT 代表覆寫同名檔案，避免檔案無限膨脹
                string sql = $@"
                    BACKUP DATABASE [{database}] 
                    TO DISK = @BackupPath 
                    WITH FORMAT, INIT, COMPRESSION;";

                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                // 備份時間可能較長，將 CommandTimeout 設為 0 (無限制)
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 255) { Value = backupPath });

                await cmd.ExecuteNonQueryAsync();

                AppendLog("✅ 備份成功！檔案已儲存至：" + backupPath);
                MessageBox.Show("資料庫備份成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"❌ 備份失敗：{ex.Message}");
                MessageBox.Show($"備份發生嚴重異常：\n{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBackup.Enabled = true;
            }
        }

        // =====================================================================
        // ⚠️ [還原引擎] 上帝視角暴力踢人與覆寫 (Single-User Rollback & Restore)
        // =====================================================================
        private async void BtnRestore_Click(object? sender, EventArgs e)
        {
            string server = txtServer.Text.Trim();
            string database = txtDatabase.Text.Trim();
            string restorePath = txtRestorePath.Text.Trim();

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(restorePath))
            {
                MessageBox.Show("請確認伺服器、資料庫與還原檔案路徑皆已填寫！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(restorePath))
            {
                MessageBox.Show("指定的備份檔案不存在，請重新選擇！", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 🚨 終極防呆：再次確認意圖，因為此操作絕對不可逆！
            if (MessageBox.Show($"您即將用檔案 [{Path.GetFileName(restorePath)}] 覆寫資料庫 [{database}]。\n\n這將會強制踢出所有線上使用者，且目前的資料將永久遺失！\n\n您確定要繼續嗎？",
                "危險操作確認", MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                AppendLog("已取消還原作業。");
                return;
            }

            btnRestore.Enabled = false;
            AppendLog($"開始執行資料庫 [{database}] 還原作業...");
            AppendLog("正在強制中斷所有線上連線...");

            try
            {
                // =====================================================================
                // 💡 核心架構切換：連線至 'master' 系統資料庫！
                // 絕對不能連線到目標資料庫本身去還原它自己，會引發「資料庫使用中」死鎖。
                // =====================================================================
                string masterConnStr = $"Server={server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

                using var conn = new SqlConnection(masterConnStr);
                await conn.OpenAsync();

                // 🚨 步驟 1：暴力踢人 (ROLLBACK IMMEDIATE)
                // 強制將資料庫切換為單人模式，並瞬間退回所有正在執行的交易
                string killSql = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                using (var cmdKill = new SqlCommand(killSql, conn))
                {
                    await cmdKill.ExecuteNonQueryAsync();
                    AppendLog("線上連線已全數強制中斷。");
                }

                // 🚨 步驟 2：執行物理還原 (WITH REPLACE 允許覆寫)
                AppendLog("正在覆寫資料庫實體檔案...");
                string restoreSql = $@"
                    RESTORE DATABASE [{database}] 
                    FROM DISK = @RestorePath 
                    WITH REPLACE;";
                using (var cmdRestore = new SqlCommand(restoreSql, conn))
                {
                    cmdRestore.CommandTimeout = 0; // 還原極耗時，解除 Timeout 限制
                    cmdRestore.Parameters.Add(new SqlParameter("@RestorePath", SqlDbType.NVarChar, 255) { Value = restorePath });
                    await cmdRestore.ExecuteNonQueryAsync();
                }

                // 🚨 步驟 3：重新開門營業 (MULTI_USER)
                string openSql = $"ALTER DATABASE [{database}] SET MULTI_USER;";
                using (var cmdOpen = new SqlCommand(openSql, conn))
                {
                    await cmdOpen.ExecuteNonQueryAsync();
                }

                AppendLog("✅ 還原成功！資料庫已重新開放連線。");
                MessageBox.Show("資料庫還原成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // 發生錯誤時，盡力嘗試將資料庫解鎖，避免卡在 SINGLE_USER 模式
                try
                {
                    string emergencyOpenStr = $"Server={server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
                    using var emergencyConn = new SqlConnection(emergencyOpenStr);
                    await emergencyConn.OpenAsync();
                    using var cmdEmergency = new SqlCommand($"ALTER DATABASE [{database}] SET MULTI_USER;", emergencyConn);
                    await cmdEmergency.ExecuteNonQueryAsync();
                    AppendLog("已嘗試將資料庫緊急解除單人模式鎖定。");
                }
                catch { /* 忽略緊急解鎖時的錯誤 */ }

                AppendLog($"❌ 還原失敗：{ex.Message}");
                MessageBox.Show($"還原發生嚴重異常：\n{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRestore.Enabled = true;
            }
        }
    }
}