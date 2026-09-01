namespace ERPLAB.UI.Views.BaseData
{
    partial class EmployeePage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EmployeePage));
            tsActions = new ToolStrip();
            btnRefresh = new ToolStripButton();
            btnCancel = new ToolStripButton();
            btnSave = new ToolStripButton();
            btnEdit = new ToolStripButton();
            btnAdd = new ToolStripButton();
            splitContainerMain = new SplitContainer();
            dgvEmployees = new DataGridView();
            ucPagination = new ERPLAB.UI.Core.PaginationControl();
            pnlSearch = new Panel();
            btnSearch = new Button();
            txtKeyword = new TextBox();
            chkShowInactive = new CheckBox();
            panel1 = new Panel();
            cmbJobTitle = new ComboBox();
            cmbJobStatus = new ComboBox();
            label11 = new Label();
            label10 = new Label();
            cmbDistrict = new ComboBox();
            cmbCity = new ComboBox();
            txtZipRear = new TextBox();
            cmbGender = new ComboBox();
            txtEmail = new TextBox();
            txtAddress = new TextBox();
            txtZipFront = new TextBox();
            txtPhoneNumber = new TextBox();
            txtEmployeeName = new TextBox();
            txtEmployeeNo = new TextBox();
            lblStatusBadge = new Label();
            label8 = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            lblAuditTrail = new Label();
            tsActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvEmployees).BeginInit();
            pnlSearch.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tsActions
            // 
            tsActions.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            tsActions.GripStyle = ToolStripGripStyle.Hidden;
            tsActions.ImageScalingSize = new Size(28, 28);
            tsActions.Items.AddRange(new ToolStripItem[] { btnRefresh, btnCancel, btnSave, btnEdit, btnAdd });
            tsActions.Location = new Point(0, 0);
            tsActions.Name = "tsActions";
            tsActions.Size = new Size(1073, 35);
            tsActions.TabIndex = 0;
            tsActions.Text = "toolStrip1";
            // 
            // btnRefresh
            // 
            btnRefresh.Alignment = ToolStripItemAlignment.Right;
            btnRefresh.Image = (Image)resources.GetObject("btnRefresh.Image");
            btnRefresh.ImageTransparentColor = Color.Magenta;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(73, 32);
            btnRefresh.Text = "重整";
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnCancel
            // 
            btnCancel.Alignment = ToolStripItemAlignment.Right;
            btnCancel.Enabled = false;
            btnCancel.Image = (Image)resources.GetObject("btnCancel.Image");
            btnCancel.ImageTransparentColor = Color.Magenta;
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(73, 32);
            btnCancel.Text = "取消";
            // 
            // btnSave
            // 
            btnSave.Alignment = ToolStripItemAlignment.Right;
            btnSave.Enabled = false;
            btnSave.Image = (Image)resources.GetObject("btnSave.Image");
            btnSave.ImageTransparentColor = Color.Magenta;
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(73, 32);
            btnSave.Text = "存檔";
            // 
            // btnEdit
            // 
            btnEdit.Alignment = ToolStripItemAlignment.Right;
            btnEdit.Image = (Image)resources.GetObject("btnEdit.Image");
            btnEdit.ImageTransparentColor = Color.Magenta;
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(93, 32);
            btnEdit.Text = "✏️ 修改";
            // 
            // btnAdd
            // 
            btnAdd.Alignment = ToolStripItemAlignment.Right;
            btnAdd.Image = (Image)resources.GetObject("btnAdd.Image");
            btnAdd.ImageTransparentColor = Color.Magenta;
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(73, 32);
            btnAdd.Text = "新增";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel2;
            splitContainerMain.IsSplitterFixed = true;
            splitContainerMain.Location = new Point(0, 35);
            splitContainerMain.MinimumSize = new Size(760, 400);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(dgvEmployees);
            splitContainerMain.Panel1.Controls.Add(ucPagination);
            splitContainerMain.Panel1.Controls.Add(pnlSearch);
            splitContainerMain.Panel1.RightToLeft = RightToLeft.No;
            splitContainerMain.Panel1MinSize = 50;
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.AutoScroll = true;
            splitContainerMain.Panel2.Controls.Add(panel1);
            splitContainerMain.Panel2.RightToLeft = RightToLeft.No;
            splitContainerMain.Panel2MinSize = 250;
            splitContainerMain.Size = new Size(1073, 565);
            splitContainerMain.SplitterDistance = 383;
            splitContainerMain.SplitterIncrement = 5;
            splitContainerMain.SplitterWidth = 6;
            splitContainerMain.TabIndex = 1;
            // 
            // dgvEmployees
            // 
            dgvEmployees.AllowUserToAddRows = false;
            dgvEmployees.AllowUserToDeleteRows = false;
            dgvEmployees.AllowUserToResizeRows = false;
            dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvEmployees.BackgroundColor = Color.White;
            dgvEmployees.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvEmployees.Dock = DockStyle.Fill;
            dgvEmployees.Location = new Point(0, 50);
            dgvEmployees.MultiSelect = false;
            dgvEmployees.Name = "dgvEmployees";
            dgvEmployees.ReadOnly = true;
            dgvEmployees.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmployees.Size = new Size(383, 477);
            dgvEmployees.TabIndex = 0;
            // 
            // ucPagination
            // 
            ucPagination.Dock = DockStyle.Bottom;
            ucPagination.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            ucPagination.Location = new Point(0, 527);
            ucPagination.Margin = new Padding(4);
            ucPagination.Name = "ucPagination";
            ucPagination.Size = new Size(383, 38);
            ucPagination.TabIndex = 23;
            // 
            // pnlSearch
            // 
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtKeyword);
            pnlSearch.Controls.Add(chkShowInactive);
            pnlSearch.Dock = DockStyle.Top;
            pnlSearch.Location = new Point(0, 0);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new Size(383, 50);
            pnlSearch.TabIndex = 1;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(145, 10);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(71, 30);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "🔍 搜尋";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // txtKeyword
            // 
            txtKeyword.Location = new Point(3, 11);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(126, 29);
            txtKeyword.TabIndex = 1;
            // 
            // chkShowInactive
            // 
            chkShowInactive.AutoSize = true;
            chkShowInactive.Cursor = Cursors.Hand;
            chkShowInactive.Location = new Point(237, 13);
            chkShowInactive.Name = "chkShowInactive";
            chkShowInactive.Size = new Size(140, 24);
            chkShowInactive.TabIndex = 0;
            chkShowInactive.Text = "包含已停用員工";
            chkShowInactive.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(cmbJobTitle);
            panel1.Controls.Add(cmbJobStatus);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(cmbDistrict);
            panel1.Controls.Add(cmbCity);
            panel1.Controls.Add(txtZipRear);
            panel1.Controls.Add(cmbGender);
            panel1.Controls.Add(txtEmail);
            panel1.Controls.Add(txtAddress);
            panel1.Controls.Add(txtZipFront);
            panel1.Controls.Add(txtPhoneNumber);
            panel1.Controls.Add(txtEmployeeName);
            panel1.Controls.Add(txtEmployeeNo);
            panel1.Controls.Add(lblStatusBadge);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(lblAuditTrail);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(660, 559);
            panel1.TabIndex = 24;
            // 
            // cmbJobTitle
            // 
            cmbJobTitle.BackColor = Color.LightBlue;
            cmbJobTitle.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbJobTitle.Enabled = false;
            cmbJobTitle.FlatStyle = FlatStyle.Popup;
            cmbJobTitle.FormattingEnabled = true;
            cmbJobTitle.Location = new Point(151, 149);
            cmbJobTitle.Name = "cmbJobTitle";
            cmbJobTitle.Size = new Size(108, 28);
            cmbJobTitle.TabIndex = 27;
            // 
            // cmbJobStatus
            // 
            cmbJobStatus.BackColor = Color.LightBlue;
            cmbJobStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbJobStatus.Enabled = false;
            cmbJobStatus.FlatStyle = FlatStyle.Popup;
            cmbJobStatus.FormattingEnabled = true;
            cmbJobStatus.Location = new Point(151, 107);
            cmbJobStatus.Name = "cmbJobStatus";
            cmbJobStatus.Size = new Size(108, 28);
            cmbJobStatus.TabIndex = 26;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(24, 152);
            label11.Name = "label11";
            label11.Size = new Size(73, 20);
            label11.TabIndex = 25;
            label11.Text = "職務名稱";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(24, 322);
            label10.Name = "label10";
            label10.Size = new Size(73, 20);
            label10.TabIndex = 24;
            label10.Text = "行政區域";
            // 
            // cmbDistrict
            // 
            cmbDistrict.BackColor = Color.LightBlue;
            cmbDistrict.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDistrict.Enabled = false;
            cmbDistrict.FlatStyle = FlatStyle.Popup;
            cmbDistrict.FormattingEnabled = true;
            cmbDistrict.Location = new Point(273, 319);
            cmbDistrict.Name = "cmbDistrict";
            cmbDistrict.Size = new Size(108, 28);
            cmbDistrict.TabIndex = 22;
            // 
            // cmbCity
            // 
            cmbCity.BackColor = Color.LightBlue;
            cmbCity.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCity.Enabled = false;
            cmbCity.FlatStyle = FlatStyle.Popup;
            cmbCity.FormattingEnabled = true;
            cmbCity.Location = new Point(151, 319);
            cmbCity.Name = "cmbCity";
            cmbCity.Size = new Size(108, 28);
            cmbCity.TabIndex = 21;
            // 
            // txtZipRear
            // 
            txtZipRear.Location = new Point(197, 361);
            txtZipRear.MaxLength = 3;
            txtZipRear.Name = "txtZipRear";
            txtZipRear.ReadOnly = true;
            txtZipRear.Size = new Size(40, 29);
            txtZipRear.TabIndex = 20;
            // 
            // cmbGender
            // 
            cmbGender.BackColor = Color.LightBlue;
            cmbGender.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGender.Enabled = false;
            cmbGender.FlatStyle = FlatStyle.Popup;
            cmbGender.FormattingEnabled = true;
            cmbGender.Location = new Point(151, 191);
            cmbGender.Name = "cmbGender";
            cmbGender.Size = new Size(108, 28);
            cmbGender.TabIndex = 18;
            // 
            // txtEmail
            // 
            txtEmail.Location = new Point(151, 233);
            txtEmail.MaxLength = 100;
            txtEmail.Name = "txtEmail";
            txtEmail.ReadOnly = true;
            txtEmail.Size = new Size(339, 29);
            txtEmail.TabIndex = 16;
            // 
            // txtAddress
            // 
            txtAddress.BackColor = Color.LightBlue;
            txtAddress.Location = new Point(151, 404);
            txtAddress.MaxLength = 200;
            txtAddress.Multiline = true;
            txtAddress.Name = "txtAddress";
            txtAddress.ReadOnly = true;
            txtAddress.Size = new Size(339, 58);
            txtAddress.TabIndex = 15;
            // 
            // txtZipFront
            // 
            txtZipFront.Location = new Point(151, 361);
            txtZipFront.MaxLength = 3;
            txtZipFront.Name = "txtZipFront";
            txtZipFront.ReadOnly = true;
            txtZipFront.Size = new Size(40, 29);
            txtZipFront.TabIndex = 14;
            // 
            // txtPhoneNumber
            // 
            txtPhoneNumber.BackColor = Color.LightBlue;
            txtPhoneNumber.Location = new Point(151, 276);
            txtPhoneNumber.MaxLength = 20;
            txtPhoneNumber.Name = "txtPhoneNumber";
            txtPhoneNumber.ReadOnly = true;
            txtPhoneNumber.Size = new Size(339, 29);
            txtPhoneNumber.TabIndex = 13;
            // 
            // txtEmployeeName
            // 
            txtEmployeeName.BackColor = Color.LightBlue;
            txtEmployeeName.Location = new Point(151, 64);
            txtEmployeeName.MaxLength = 50;
            txtEmployeeName.Name = "txtEmployeeName";
            txtEmployeeName.ReadOnly = true;
            txtEmployeeName.Size = new Size(339, 29);
            txtEmployeeName.TabIndex = 11;
            // 
            // txtEmployeeNo
            // 
            txtEmployeeNo.BackColor = SystemColors.Control;
            txtEmployeeNo.Location = new Point(151, 21);
            txtEmployeeNo.MaxLength = 20;
            txtEmployeeNo.Name = "txtEmployeeNo";
            txtEmployeeNo.ReadOnly = true;
            txtEmployeeNo.Size = new Size(139, 29);
            txtEmployeeNo.TabIndex = 10;
            // 
            // lblStatusBadge
            // 
            lblStatusBadge.AutoSize = true;
            lblStatusBadge.Location = new Point(308, 24);
            lblStatusBadge.Name = "lblStatusBadge";
            lblStatusBadge.Size = new Size(73, 20);
            lblStatusBadge.TabIndex = 9;
            lblStatusBadge.Text = "啟用狀態";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(24, 236);
            label8.Name = "label8";
            label8.Size = new Size(73, 20);
            label8.TabIndex = 7;
            label8.Text = "電子信箱";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(24, 407);
            label7.Name = "label7";
            label7.Size = new Size(73, 20);
            label7.TabIndex = 6;
            label7.Text = "詳細地址";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(24, 364);
            label6.Name = "label6";
            label6.Size = new Size(77, 20);
            label6.TabIndex = 5;
            label6.Text = "郵遞區號 ";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(24, 194);
            label5.Name = "label5";
            label5.Size = new Size(73, 20);
            label5.TabIndex = 4;
            label5.Text = "性        別";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(24, 279);
            label4.Name = "label4";
            label4.Size = new Size(73, 20);
            label4.TabIndex = 3;
            label4.Text = "聯絡電話";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(24, 110);
            label3.Name = "label3";
            label3.Size = new Size(73, 20);
            label3.TabIndex = 2;
            label3.Text = "在職狀態";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 67);
            label2.Name = "label2";
            label2.Size = new Size(73, 20);
            label2.TabIndex = 1;
            label2.Text = "員工姓名";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 24);
            label1.Name = "label1";
            label1.Size = new Size(73, 20);
            label1.TabIndex = 0;
            label1.Text = "員工編號";
            // 
            // lblAuditTrail
            // 
            lblAuditTrail.Font = new Font("微軟正黑體", 9F, FontStyle.Regular, GraphicsUnit.Point, 136);
            lblAuditTrail.ForeColor = Color.Gray;
            lblAuditTrail.Location = new Point(3, 540);
            lblAuditTrail.Margin = new Padding(3);
            lblAuditTrail.Name = "lblAuditTrail";
            lblAuditTrail.Size = new Size(654, 16);
            lblAuditTrail.TabIndex = 23;
            lblAuditTrail.Text = "lblAuditTrail";
            lblAuditTrail.TextAlign = ContentAlignment.MiddleRight;
            // 
            // EmployeePage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(splitContainerMain);
            Controls.Add(tsActions);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            MinimumSize = new Size(760, 400);
            Name = "EmployeePage";
            Size = new Size(1073, 600);
            tsActions.ResumeLayout(false);
            tsActions.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvEmployees).EndInit();
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip tsActions;
        private ToolStripButton btnAdd;
        private ToolStripButton btnEdit;
        private ToolStripButton btnSave;
        private ToolStripButton btnCancel;
        private ToolStripButton btnRefresh;
        private SplitContainer splitContainerMain;
        private DataGridView dgvEmployees;
        private Panel pnlSearch;
        private CheckBox chkShowInactive;
        private Button btnSearch;
        private TextBox txtKeyword;
        private Core.PaginationControl ucPagination;
        private ComboBox cmbDistrict;
        private ComboBox cmbCity;
        private TextBox txtZipRear;
        private ComboBox cmbGender;
        private TextBox txtEmail;
        private TextBox txtAddress;
        private TextBox txtZipFront;
        private TextBox txtPhoneNumber;
        private TextBox txtEmployeeName;
        private TextBox txtEmployeeNo;
        private Label lblStatusBadge;
        private Label label8;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label lblAuditTrail;
        private Panel panel1;
        private Label label10;
        private ComboBox cmbJobTitle;
        private ComboBox cmbJobStatus;
        private Label label11;
    }
}
