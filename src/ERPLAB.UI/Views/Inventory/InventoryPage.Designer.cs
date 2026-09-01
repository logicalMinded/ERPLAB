namespace ERPLAB.UI.Views.Inventory
{
    partial class InventoryPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InventoryPage));
            lblAuditTrail = new Label();
            pnlSearch = new Panel();
            btnSearch = new Button();
            txtKeyword = new TextBox();
            ucPagination = new ERPLAB.UI.Core.PaginationControl();
            splitMain = new SplitContainer();
            dgvInventoryMaster = new DataGridView();
            splitRight = new SplitContainer();
            groupBox1 = new GroupBox();
            lblTotalDiffAmount = new Label();
            groupBox2 = new GroupBox();
            label9 = new Label();
            txtEmployeeName = new TextBox();
            btnLookupEmployee = new Button();
            txtEmployeeNo = new TextBox();
            label4 = new Label();
            txtRemark = new TextBox();
            label7 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            dtpInventoryDate = new DateTimePicker();
            lblStatusBadge = new Label();
            txtInventoryNo = new TextBox();
            dgvInventoryDetail = new DataGridView();
            tsDetailActions = new ToolStrip();
            btnLoadSystemStock = new ToolStripButton();
            btnClearDetails = new ToolStripButton();
            btnRefresh = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            btnDelete = new ToolStripButton();
            btnPost = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnCancel = new ToolStripButton();
            btnSave = new ToolStripButton();
            btnEdit = new ToolStripButton();
            tsActions = new ToolStrip();
            btnAdd = new ToolStripButton();
            pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInventoryMaster).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitRight).BeginInit();
            splitRight.Panel1.SuspendLayout();
            splitRight.Panel2.SuspendLayout();
            splitRight.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInventoryDetail).BeginInit();
            tsDetailActions.SuspendLayout();
            tsActions.SuspendLayout();
            SuspendLayout();
            // 
            // lblAuditTrail
            // 
            lblAuditTrail.AutoSize = true;
            lblAuditTrail.Font = new Font("微軟正黑體", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 136);
            lblAuditTrail.ForeColor = Color.Gray;
            lblAuditTrail.Location = new Point(19, 291);
            lblAuditTrail.Name = "lblAuditTrail";
            lblAuditTrail.Size = new Size(80, 17);
            lblAuditTrail.TabIndex = 25;
            lblAuditTrail.Text = "lblAuditTrail";
            // 
            // pnlSearch
            // 
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtKeyword);
            pnlSearch.Dock = DockStyle.Top;
            pnlSearch.Location = new Point(0, 0);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new Size(533, 40);
            pnlSearch.TabIndex = 0;
            // 
            // btnSearch
            // 
            btnSearch.AutoSize = true;
            btnSearch.Location = new Point(222, 5);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 30);
            btnSearch.TabIndex = 1;
            btnSearch.Text = "🔍 搜尋";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // txtKeyword
            // 
            txtKeyword.Location = new Point(15, 6);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(168, 29);
            txtKeyword.TabIndex = 0;
            // 
            // ucPagination
            // 
            ucPagination.Dock = DockStyle.Bottom;
            ucPagination.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            ucPagination.Location = new Point(0, 622);
            ucPagination.Margin = new Padding(4);
            ucPagination.Name = "ucPagination";
            ucPagination.Size = new Size(533, 40);
            ucPagination.TabIndex = 2;
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.FixedPanel = FixedPanel.Panel1;
            splitMain.Location = new Point(0, 35);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(dgvInventoryMaster);
            splitMain.Panel1.Controls.Add(ucPagination);
            splitMain.Panel1.Controls.Add(pnlSearch);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(splitRight);
            splitMain.Size = new Size(1417, 662);
            splitMain.SplitterDistance = 533;
            splitMain.TabIndex = 3;
            // 
            // dgvInventoryMaster
            // 
            dgvInventoryMaster.AllowUserToAddRows = false;
            dgvInventoryMaster.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInventoryMaster.Dock = DockStyle.Fill;
            dgvInventoryMaster.Location = new Point(0, 40);
            dgvInventoryMaster.Name = "dgvInventoryMaster";
            dgvInventoryMaster.ReadOnly = true;
            dgvInventoryMaster.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInventoryMaster.Size = new Size(533, 582);
            dgvInventoryMaster.TabIndex = 1;
            // 
            // splitRight
            // 
            splitRight.Dock = DockStyle.Fill;
            splitRight.FixedPanel = FixedPanel.Panel1;
            splitRight.Location = new Point(0, 0);
            splitRight.Name = "splitRight";
            splitRight.Orientation = Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            splitRight.Panel1.AutoScroll = true;
            splitRight.Panel1.Controls.Add(groupBox1);
            // 
            // splitRight.Panel2
            // 
            splitRight.Panel2.Controls.Add(dgvInventoryDetail);
            splitRight.Panel2.Controls.Add(tsDetailActions);
            splitRight.Size = new Size(880, 662);
            splitRight.SplitterDistance = 320;
            splitRight.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblTotalDiffAmount);
            groupBox1.Controls.Add(groupBox2);
            groupBox1.Controls.Add(lblAuditTrail);
            groupBox1.Controls.Add(txtRemark);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(dtpInventoryDate);
            groupBox1.Controls.Add(lblStatusBadge);
            groupBox1.Controls.Add(txtInventoryNo);
            groupBox1.Location = new Point(0, 0);
            groupBox1.MinimumSize = new Size(880, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(880, 320);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "盤點單主檔";
            // 
            // lblTotalDiffAmount
            // 
            lblTotalDiffAmount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTotalDiffAmount.ForeColor = Color.Blue;
            lblTotalDiffAmount.Location = new Point(544, 291);
            lblTotalDiffAmount.Name = "lblTotalDiffAmount";
            lblTotalDiffAmount.Size = new Size(330, 20);
            lblTotalDiffAmount.TabIndex = 27;
            lblTotalDiffAmount.Text = "lblTotalDiffAmount";
            lblTotalDiffAmount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(txtEmployeeName);
            groupBox2.Controls.Add(btnLookupEmployee);
            groupBox2.Controls.Add(txtEmployeeNo);
            groupBox2.Controls.Add(label4);
            groupBox2.Location = new Point(6, 85);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(868, 87);
            groupBox2.TabIndex = 26;
            groupBox2.TabStop = false;
            groupBox2.Text = "盤點人員";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(362, 32);
            label9.Name = "label9";
            label9.Size = new Size(73, 20);
            label9.TabIndex = 29;
            label9.Text = "員工姓名";
            // 
            // txtEmployeeName
            // 
            txtEmployeeName.Location = new Point(469, 29);
            txtEmployeeName.Name = "txtEmployeeName";
            txtEmployeeName.ReadOnly = true;
            txtEmployeeName.Size = new Size(344, 29);
            txtEmployeeName.TabIndex = 28;
            // 
            // btnLookupEmployee
            // 
            btnLookupEmployee.AutoSize = true;
            btnLookupEmployee.Location = new Point(269, 28);
            btnLookupEmployee.Name = "btnLookupEmployee";
            btnLookupEmployee.Size = new Size(35, 30);
            btnLookupEmployee.TabIndex = 27;
            btnLookupEmployee.Text = "🔍";
            btnLookupEmployee.UseVisualStyleBackColor = true;
            // 
            // txtEmployeeNo
            // 
            txtEmployeeNo.BackColor = Color.LightBlue;
            txtEmployeeNo.Location = new Point(110, 29);
            txtEmployeeNo.Name = "txtEmployeeNo";
            txtEmployeeNo.ReadOnly = true;
            txtEmployeeNo.Size = new Size(153, 29);
            txtEmployeeNo.TabIndex = 26;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(13, 33);
            label4.Name = "label4";
            label4.Size = new Size(73, 20);
            label4.TabIndex = 25;
            label4.Text = "員工編號";
            // 
            // txtRemark
            // 
            txtRemark.Location = new Point(113, 198);
            txtRemark.Multiline = true;
            txtRemark.Name = "txtRemark";
            txtRemark.ReadOnly = true;
            txtRemark.Size = new Size(761, 87);
            txtRemark.TabIndex = 16;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(19, 201);
            label7.Name = "label7";
            label7.Size = new Size(73, 20);
            label7.TabIndex = 9;
            label7.Text = "單據備註";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(368, 40);
            label3.Name = "label3";
            label3.Size = new Size(89, 20);
            label3.TabIndex = 5;
            label3.Text = "盤點基準日";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(661, 40);
            label2.Name = "label2";
            label2.Size = new Size(73, 20);
            label2.TabIndex = 4;
            label2.Text = "單據狀態";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(19, 40);
            label1.Name = "label1";
            label1.Size = new Size(73, 20);
            label1.TabIndex = 3;
            label1.Text = "單據編號";
            // 
            // dtpInventoryDate
            // 
            dtpInventoryDate.Enabled = false;
            dtpInventoryDate.Format = DateTimePickerFormat.Short;
            dtpInventoryDate.Location = new Point(486, 34);
            dtpInventoryDate.Name = "dtpInventoryDate";
            dtpInventoryDate.Size = new Size(126, 29);
            dtpInventoryDate.TabIndex = 2;
            // 
            // lblStatusBadge
            // 
            lblStatusBadge.AutoSize = true;
            lblStatusBadge.Location = new Point(740, 40);
            lblStatusBadge.Name = "lblStatusBadge";
            lblStatusBadge.Size = new Size(122, 20);
            lblStatusBadge.TabIndex = 1;
            lblStatusBadge.Text = "lblStatusBadge";
            // 
            // txtInventoryNo
            // 
            txtInventoryNo.Location = new Point(113, 37);
            txtInventoryNo.Name = "txtInventoryNo";
            txtInventoryNo.ReadOnly = true;
            txtInventoryNo.Size = new Size(194, 29);
            txtInventoryNo.TabIndex = 0;
            // 
            // dgvInventoryDetail
            // 
            dgvInventoryDetail.AllowUserToResizeRows = false;
            dgvInventoryDetail.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInventoryDetail.Dock = DockStyle.Fill;
            dgvInventoryDetail.Location = new Point(0, 27);
            dgvInventoryDetail.Name = "dgvInventoryDetail";
            dgvInventoryDetail.Size = new Size(880, 311);
            dgvInventoryDetail.TabIndex = 0;
            // 
            // tsDetailActions
            // 
            tsDetailActions.Items.AddRange(new ToolStripItem[] { btnLoadSystemStock, btnClearDetails });
            tsDetailActions.Location = new Point(0, 0);
            tsDetailActions.Name = "tsDetailActions";
            tsDetailActions.Size = new Size(880, 27);
            tsDetailActions.TabIndex = 1;
            tsDetailActions.Text = "toolStrip1";
            // 
            // btnLoadSystemStock
            // 
            btnLoadSystemStock.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnLoadSystemStock.Enabled = false;
            btnLoadSystemStock.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            btnLoadSystemStock.Image = (Image)resources.GetObject("btnLoadSystemStock.Image");
            btnLoadSystemStock.ImageTransparentColor = Color.Magenta;
            btnLoadSystemStock.Name = "btnLoadSystemStock";
            btnLoadSystemStock.Size = new Size(129, 24);
            btnLoadSystemStock.Text = "📥 載入帳面庫存";
            // 
            // btnClearDetails
            // 
            btnClearDetails.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnClearDetails.Enabled = false;
            btnClearDetails.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            btnClearDetails.Image = (Image)resources.GetObject("btnClearDetails.Image");
            btnClearDetails.ImageTransparentColor = Color.Magenta;
            btnClearDetails.Name = "btnClearDetails";
            btnClearDetails.Size = new Size(129, 24);
            btnClearDetails.Text = "\U0001f9f9 清空所有明細";
            // 
            // btnRefresh
            // 
            btnRefresh.Alignment = ToolStripItemAlignment.Right;
            btnRefresh.Image = (Image)resources.GetObject("btnRefresh.Image");
            btnRefresh.ImageTransparentColor = Color.Magenta;
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(73, 32);
            btnRefresh.Text = "重整";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 35);
            // 
            // btnDelete
            // 
            btnDelete.Alignment = ToolStripItemAlignment.Right;
            btnDelete.Image = (Image)resources.GetObject("btnDelete.Image");
            btnDelete.ImageTransparentColor = Color.Magenta;
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(105, 32);
            btnDelete.Text = "刪除草稿";
            // 
            // btnPost
            // 
            btnPost.Alignment = ToolStripItemAlignment.Right;
            btnPost.Image = (Image)resources.GetObject("btnPost.Image");
            btnPost.ImageTransparentColor = Color.Magenta;
            btnPost.Name = "btnPost";
            btnPost.Size = new Size(105, 32);
            btnPost.Text = "審核過帳";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 35);
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
            btnEdit.Size = new Size(73, 32);
            btnEdit.Text = "修改";
            // 
            // tsActions
            // 
            tsActions.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            tsActions.ImageScalingSize = new Size(28, 28);
            tsActions.Items.AddRange(new ToolStripItem[] { btnRefresh, toolStripSeparator2, btnDelete, btnPost, toolStripSeparator1, btnCancel, btnSave, btnEdit, btnAdd });
            tsActions.Location = new Point(0, 0);
            tsActions.Name = "tsActions";
            tsActions.Size = new Size(1417, 35);
            tsActions.TabIndex = 2;
            tsActions.Text = "toolStrip1";
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
            // InventoryPage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitMain);
            Controls.Add(tsActions);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "InventoryPage";
            Size = new Size(1417, 697);
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvInventoryMaster).EndInit();
            splitRight.Panel1.ResumeLayout(false);
            splitRight.Panel2.ResumeLayout(false);
            splitRight.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).EndInit();
            splitRight.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInventoryDetail).EndInit();
            tsDetailActions.ResumeLayout(false);
            tsDetailActions.PerformLayout();
            tsActions.ResumeLayout(false);
            tsActions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblAuditTrail;
        private Panel pnlSearch;
        private Button btnSearch;
        private TextBox txtKeyword;
        private Core.PaginationControl ucPagination;
        private SplitContainer splitMain;
        private DataGridView dgvInventoryMaster;
        private SplitContainer splitRight;
        private GroupBox groupBox1;
        private TextBox txtRemark;
        private Label label7;
        private Label label3;
        private Label label2;
        private Label label1;
        private DateTimePicker dtpInventoryDate;
        private Label lblStatusBadge;
        private TextBox txtInventoryNo;
        private DataGridView dgvInventoryDetail;
        private ToolStrip tsDetailActions;
        private ToolStripButton btnLoadSystemStock;
        private ToolStripButton btnRefresh;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton btnDelete;
        private ToolStripButton btnPost;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnCancel;
        private ToolStripButton btnSave;
        private ToolStripButton btnEdit;
        private ToolStrip tsActions;
        private ToolStripButton btnAdd;
        private GroupBox groupBox2;
        private Label label9;
        private TextBox txtEmployeeName;
        private Button btnLookupEmployee;
        private TextBox txtEmployeeNo;
        private Label label4;
        private ToolStripButton btnClearDetails;
        private Label lblTotalDiffAmount;
    }
}
