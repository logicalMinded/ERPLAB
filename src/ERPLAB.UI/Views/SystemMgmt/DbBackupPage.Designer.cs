namespace ERPLAB.UI.Views.SystemMgmt
{
    partial class DbBackupPage
    {
        /// <summary> 
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        /// <summary> 
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            gbConnection = new GroupBox();
            txtDatabase = new TextBox();
            txtServer = new TextBox();
            label4 = new Label();
            label1 = new Label();
            gbBackup = new GroupBox();
            btnBackup = new Button();
            btnBrowseBackupPath = new Button();
            txtBackupPath = new TextBox();
            label2 = new Label();
            gbRestore = new GroupBox();
            lblRestoreWarning = new Label();
            btnRestore = new Button();
            btnBrowseRestorePath = new Button();
            label3 = new Label();
            txtRestorePath = new TextBox();
            txtLog = new TextBox();
            gbConnection.SuspendLayout();
            gbBackup.SuspendLayout();
            gbRestore.SuspendLayout();
            SuspendLayout();
            // 
            // gbConnection
            // 
            gbConnection.Controls.Add(txtDatabase);
            gbConnection.Controls.Add(txtServer);
            gbConnection.Controls.Add(label4);
            gbConnection.Controls.Add(label1);
            gbConnection.Location = new Point(115, 16);
            gbConnection.Name = "gbConnection";
            gbConnection.Size = new Size(671, 135);
            gbConnection.TabIndex = 0;
            gbConnection.TabStop = false;
            gbConnection.Text = "資料庫連線設定";
            // 
            // txtDatabase
            // 
            txtDatabase.Location = new Point(129, 88);
            txtDatabase.Name = "txtDatabase";
            txtDatabase.Size = new Size(170, 29);
            txtDatabase.TabIndex = 3;
            txtDatabase.Text = "ERPLAB2026";
            // 
            // txtServer
            // 
            txtServer.Location = new Point(129, 41);
            txtServer.Name = "txtServer";
            txtServer.Size = new Size(170, 29);
            txtServer.TabIndex = 2;
            txtServer.Text = ".\\SQL2022";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(34, 91);
            label4.Name = "label4";
            label4.Size = new Size(89, 20);
            label4.TabIndex = 1;
            label4.Text = "資料庫名稱";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(34, 44);
            label1.Name = "label1";
            label1.Size = new Size(89, 20);
            label1.TabIndex = 0;
            label1.Text = "伺服器名稱";
            // 
            // gbBackup
            // 
            gbBackup.Controls.Add(btnBackup);
            gbBackup.Controls.Add(btnBrowseBackupPath);
            gbBackup.Controls.Add(txtBackupPath);
            gbBackup.Controls.Add(label2);
            gbBackup.Location = new Point(115, 157);
            gbBackup.Name = "gbBackup";
            gbBackup.Size = new Size(671, 160);
            gbBackup.TabIndex = 1;
            gbBackup.TabStop = false;
            gbBackup.Text = "資料庫備份";
            // 
            // btnBackup
            // 
            btnBackup.Location = new Point(514, 110);
            btnBackup.Name = "btnBackup";
            btnBackup.Size = new Size(130, 32);
            btnBackup.TabIndex = 3;
            btnBackup.Text = "💾 執行備份";
            btnBackup.UseVisualStyleBackColor = true;
            // 
            // btnBrowseBackupPath
            // 
            btnBrowseBackupPath.Location = new Point(514, 55);
            btnBrowseBackupPath.Name = "btnBrowseBackupPath";
            btnBrowseBackupPath.Size = new Size(130, 32);
            btnBrowseBackupPath.TabIndex = 2;
            btnBrowseBackupPath.Text = " 📁 選擇路徑...";
            btnBrowseBackupPath.UseVisualStyleBackColor = true;
            // 
            // txtBackupPath
            // 
            txtBackupPath.Location = new Point(129, 58);
            txtBackupPath.Name = "txtBackupPath";
            txtBackupPath.ReadOnly = true;
            txtBackupPath.Size = new Size(382, 29);
            txtBackupPath.TabIndex = 1;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(33, 61);
            label2.Name = "label2";
            label2.Size = new Size(73, 20);
            label2.TabIndex = 0;
            label2.Text = "儲存路徑";
            // 
            // gbRestore
            // 
            gbRestore.Controls.Add(lblRestoreWarning);
            gbRestore.Controls.Add(btnRestore);
            gbRestore.Controls.Add(btnBrowseRestorePath);
            gbRestore.Controls.Add(label3);
            gbRestore.Controls.Add(txtRestorePath);
            gbRestore.Location = new Point(115, 323);
            gbRestore.Name = "gbRestore";
            gbRestore.Size = new Size(671, 197);
            gbRestore.TabIndex = 2;
            gbRestore.TabStop = false;
            gbRestore.Text = "資料庫還原";
            // 
            // lblRestoreWarning
            // 
            lblRestoreWarning.AutoSize = true;
            lblRestoreWarning.ForeColor = Color.Red;
            lblRestoreWarning.Location = new Point(33, 154);
            lblRestoreWarning.Name = "lblRestoreWarning";
            lblRestoreWarning.Size = new Size(586, 20);
            lblRestoreWarning.TabIndex = 8;
            lblRestoreWarning.Text = "※ 警告：還原將強制中斷所有使用者連線，並覆寫現有資料庫，此操作不可逆轉！";
            // 
            // btnRestore
            // 
            btnRestore.Location = new Point(514, 102);
            btnRestore.Name = "btnRestore";
            btnRestore.Size = new Size(130, 32);
            btnRestore.TabIndex = 7;
            btnRestore.Text = "⚠️ 執行還原";
            btnRestore.UseVisualStyleBackColor = true;
            // 
            // btnBrowseRestorePath
            // 
            btnBrowseRestorePath.Location = new Point(515, 48);
            btnBrowseRestorePath.Name = "btnBrowseRestorePath";
            btnBrowseRestorePath.Size = new Size(130, 32);
            btnBrowseRestorePath.TabIndex = 6;
            btnBrowseRestorePath.Text = "📁 選擇檔案...";
            btnBrowseRestorePath.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(34, 54);
            label3.Name = "label3";
            label3.Size = new Size(73, 20);
            label3.TabIndex = 4;
            label3.Text = "檔案路徑";
            // 
            // txtRestorePath
            // 
            txtRestorePath.Location = new Point(130, 51);
            txtRestorePath.Name = "txtRestorePath";
            txtRestorePath.ReadOnly = true;
            txtRestorePath.Size = new Size(382, 29);
            txtRestorePath.TabIndex = 5;
            // 
            // txtLog
            // 
            txtLog.BackColor = Color.Black;
            txtLog.ForeColor = Color.LimeGreen;
            txtLog.Location = new Point(115, 546);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(671, 239);
            txtLog.TabIndex = 3;
            // 
            // DbBackupPage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(txtLog);
            Controls.Add(gbRestore);
            Controls.Add(gbBackup);
            Controls.Add(gbConnection);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "DbBackupPage";
            Size = new Size(1143, 800);
            gbConnection.ResumeLayout(false);
            gbConnection.PerformLayout();
            gbBackup.ResumeLayout(false);
            gbBackup.PerformLayout();
            gbRestore.ResumeLayout(false);
            gbRestore.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox gbConnection;
        private TextBox txtDatabase;
        private TextBox txtServer;
        private Label label4;
        private Label label1;
        private GroupBox gbBackup;
        private GroupBox gbRestore;
        private Button btnBackup;
        private Button btnBrowseBackupPath;
        private TextBox txtBackupPath;
        private Label label2;
        private Button btnRestore;
        private Button btnBrowseRestorePath;
        private Label label3;
        private TextBox txtRestorePath;
        private Label lblRestoreWarning;
        private TextBox txtLog;
    }
}
