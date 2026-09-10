using ERPLAB.UI.Core;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ERPLAB.UI.Views.SystemMgmt
{
    /// <summary>
    /// 資料庫備份與還原維護頁面。
    /// 繼承自 BasePage，負責執行系統資料庫之備份與還原作業。
    /// 實作動態連線字串建構、T-SQL 備份壓縮、單人模式 (SINGLE_USER) 強制中斷連線還原，以及執行緒安全 (Thread-Safe) 之即時日誌輸出。
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
            // 權限檢核 (RBAC)：依據使用者授權動態限制高危險權限操作
            // =====================================================================
            RequirePermission("ACT_DB_BACKUP", btnBackup);
            RequirePermission("ACT_DB_RESTORE", btnRestore);

            // 預設參數配置，提升系統管理員操作效率
            txtServer.Text = ".\\SQL2022"; // 預設本機伺服器
            txtDatabase.Text = "ERPLAB2026"; // 預設資料庫名稱

            AppendLog("系統就緒。請確認伺服器與資料庫名稱是否正確。");
            AppendLog("警告：還原作業將強制中斷所有線上使用者連線，請謹慎操作！");
        }

        // =====================================================================
        // 終端機風格之即時日誌輸出 (Real-time Log Console)
        // =====================================================================
        private void AppendLog(string message)
        {
            // 確保跨執行緒 (Cross-Thread) 呼叫之執行緒安全性，並自動捲動至最新日誌
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => AppendLog(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret(); // 捲動至游標處
        }

        // =====================================================================
        // 備份與還原路徑選擇
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
        // 資料庫備份作業 (Hot Backup)
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
                // 動態建構連線字串，採用 Windows 整合驗證
                string connStr = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;";

                // 加入 COMPRESSION 參數啟用備份壓縮，降低硬碟 I/O 與儲存空間佔用；INIT 參數用於覆寫同名檔案
                string sql = $@"
                    BACKUP DATABASE [{database}] 
                    TO DISK = @BackupPath 
                    WITH FORMAT, INIT, COMPRESSION;";

                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                using var cmd = new SqlCommand(sql, conn);
                // 備份作業耗時較長，解除 CommandTimeout 限制
                cmd.CommandTimeout = 0;
                cmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 255) { Value = backupPath });

                await cmd.ExecuteNonQueryAsync();

                AppendLog("✅ 備份成功！檔案已儲存至：" + backupPath);
                MessageBox.Show("資料庫備份成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        // 資料庫還原作業 (Restore & Recovery)
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

            // 防呆機制：二次確認操作意圖，避免誤觸不可逆之還原作業
            if (MessageBox.Show($"您即將用檔案 [{Path.GetFileName(restorePath)}] 覆寫資料庫 [{database}]。\n\n這將會強制中斷所有線上使用者連線，且目前的資料將被覆寫！\n\n您確定要繼續嗎？",
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
                // 核心連線切換：強制連線至系統資料庫 (master) 進行操作。
                // 避免連線至目標資料庫執行還原時，引發「資料庫使用中」之例外或死鎖 (Deadlock)。
                // =====================================================================
                string masterConnStr = $"Server={server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

                using var conn = new SqlConnection(masterConnStr);
                await conn.OpenAsync();

                // 步驟一：切換至單人模式 (SINGLE_USER) 並強制中斷現有連線 (ROLLBACK IMMEDIATE)，退回所有未完成之交易
                string killSql = $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                using (var cmdKill = new SqlCommand(killSql, conn))
                {
                    await cmdKill.ExecuteNonQueryAsync();
                    AppendLog("線上連線已全數強制中斷。");
                }

                // 步驟二：執行實體檔案還原 (WITH REPLACE 允許覆寫現有資料庫)
                AppendLog("正在覆寫資料庫實體檔案...");
                string restoreSql = $@"
                    RESTORE DATABASE [{database}] 
                    FROM DISK = @RestorePath 
                    WITH REPLACE;";
                using (var cmdRestore = new SqlCommand(restoreSql, conn))
                {
                    cmdRestore.CommandTimeout = 0; // 還原作業耗時較長，解除 Timeout 限制
                    cmdRestore.Parameters.Add(new SqlParameter("@RestorePath", SqlDbType.NVarChar, 255) { Value = restorePath });
                    await cmdRestore.ExecuteNonQueryAsync();
                }

                // 步驟三：還原完成後，重新恢復多人連線模式 (MULTI_USER)
                string openSql = $"ALTER DATABASE [{database}] SET MULTI_USER;";
                using (var cmdOpen = new SqlCommand(openSql, conn))
                {
                    await cmdOpen.ExecuteNonQueryAsync();
                }

                AppendLog("✅ 還原成功！資料庫已重新開放連線。");
                MessageBox.Show("資料庫還原成功！", "系統提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // 例外處理：若還原失敗，嘗試將資料庫緊急解除單人模式鎖定，避免系統停擺
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