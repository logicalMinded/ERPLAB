namespace ERPLAB.UI.Views.Sales
{
    partial class SalesOrderPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SalesOrderPage));
            tsActions = new ToolStrip();
            btnRefresh = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            btnVoid = new ToolStripButton();
            btnPost = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnCancel = new ToolStripButton();
            btnSave = new ToolStripButton();
            btnEdit = new ToolStripButton();
            btnAdd = new ToolStripButton();
            splitMain = new SplitContainer();
            dgvSalesMaster = new DataGridView();
            ucPagination = new ERPLAB.UI.Core.PaginationControl();
            pnlSearch = new Panel();
            chkShowVoided = new CheckBox();
            btnSearch = new Button();
            txtKeyword = new TextBox();
            splitRight = new SplitContainer();
            groupBox1 = new GroupBox();
            lblAuditTrail = new Label();
            label9 = new Label();
            cmbDistrict = new ComboBox();
            cmbCity = new ComboBox();
            label8 = new Label();
            txtCustomerName = new TextBox();
            btnLookupCustomer = new Button();
            txtCustomerNo = new TextBox();
            txtRemark = new TextBox();
            txtShipAddress = new TextBox();
            txtShipZipRear = new TextBox();
            txtShipZipFront = new TextBox();
            lblTotalAmount = new Label();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            dtpSalesDate = new DateTimePicker();
            lblStatusBadge = new Label();
            txtSalesNo = new TextBox();
            dgvSalesDetail = new DataGridView();
            tsDetailActions = new ToolStrip();
            btnMoveUp = new ToolStripButton();
            btnMoveDown = new ToolStripButton();
            tsActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSalesMaster).BeginInit();
            pnlSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).BeginInit();
            splitRight.Panel1.SuspendLayout();
            splitRight.Panel2.SuspendLayout();
            splitRight.SuspendLayout();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSalesDetail).BeginInit();
            tsDetailActions.SuspendLayout();
            SuspendLayout();
            // 
            // tsActions
            // 
            tsActions.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            tsActions.ImageScalingSize = new Size(28, 28);
            tsActions.Items.AddRange(new ToolStripItem[] { btnRefresh, toolStripSeparator2, btnVoid, btnPost, toolStripSeparator1, btnCancel, btnSave, btnEdit, btnAdd });
            tsActions.Location = new Point(0, 0);
            tsActions.Name = "tsActions";
            tsActions.Size = new Size(1417, 35);
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
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Alignment = ToolStripItemAlignment.Right;
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 35);
            // 
            // btnVoid
            // 
            btnVoid.Alignment = ToolStripItemAlignment.Right;
            btnVoid.Image = (Image)resources.GetObject("btnVoid.Image");
            btnVoid.ImageTransparentColor = Color.Magenta;
            btnVoid.Name = "btnVoid";
            btnVoid.Size = new Size(116, 32);
            btnVoid.Text = " 註銷/作廢";
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
            // btnAdd
            // 
            btnAdd.Alignment = ToolStripItemAlignment.Right;
            btnAdd.Image = (Image)resources.GetObject("btnAdd.Image");
            btnAdd.ImageTransparentColor = Color.Magenta;
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(73, 32);
            btnAdd.Text = "新增";
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
            splitMain.Panel1.Controls.Add(dgvSalesMaster);
            splitMain.Panel1.Controls.Add(ucPagination);
            splitMain.Panel1.Controls.Add(pnlSearch);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(splitRight);
            splitMain.Size = new Size(1417, 662);
            splitMain.SplitterDistance = 533;
            splitMain.TabIndex = 1;
            // 
            // dgvSalesMaster
            // 
            dgvSalesMaster.AllowUserToAddRows = false;
            dgvSalesMaster.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSalesMaster.Dock = DockStyle.Fill;
            dgvSalesMaster.Location = new Point(0, 40);
            dgvSalesMaster.Name = "dgvSalesMaster";
            dgvSalesMaster.ReadOnly = true;
            dgvSalesMaster.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSalesMaster.Size = new Size(533, 582);
            dgvSalesMaster.TabIndex = 1;
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
            // pnlSearch
            // 
            pnlSearch.Controls.Add(chkShowVoided);
            pnlSearch.Controls.Add(btnSearch);
            pnlSearch.Controls.Add(txtKeyword);
            pnlSearch.Dock = DockStyle.Top;
            pnlSearch.Location = new Point(0, 0);
            pnlSearch.Name = "pnlSearch";
            pnlSearch.Size = new Size(533, 40);
            pnlSearch.TabIndex = 0;
            // 
            // chkShowVoided
            // 
            chkShowVoided.AutoSize = true;
            chkShowVoided.Location = new Point(226, 9);
            chkShowVoided.Name = "chkShowVoided";
            chkShowVoided.Size = new Size(124, 24);
            chkShowVoided.TabIndex = 2;
            chkShowVoided.Text = "包含作廢單據";
            chkShowVoided.UseVisualStyleBackColor = true;
            // 
            // btnSearch
            // 
            btnSearch.AutoSize = true;
            btnSearch.Location = new Point(145, 5);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 30);
            btnSearch.TabIndex = 1;
            btnSearch.Text = "🔍 搜尋";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // txtKeyword
            // 
            txtKeyword.Location = new Point(15, 8);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(100, 29);
            txtKeyword.TabIndex = 0;
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
            splitRight.Panel2.Controls.Add(dgvSalesDetail);
            splitRight.Panel2.Controls.Add(tsDetailActions);
            splitRight.Size = new Size(880, 662);
            splitRight.SplitterDistance = 320;
            splitRight.TabIndex = 0;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblAuditTrail);
            groupBox1.Controls.Add(label9);
            groupBox1.Controls.Add(cmbDistrict);
            groupBox1.Controls.Add(cmbCity);
            groupBox1.Controls.Add(label8);
            groupBox1.Controls.Add(txtCustomerName);
            groupBox1.Controls.Add(btnLookupCustomer);
            groupBox1.Controls.Add(txtCustomerNo);
            groupBox1.Controls.Add(txtRemark);
            groupBox1.Controls.Add(txtShipAddress);
            groupBox1.Controls.Add(txtShipZipRear);
            groupBox1.Controls.Add(txtShipZipFront);
            groupBox1.Controls.Add(lblTotalAmount);
            groupBox1.Controls.Add(label7);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(dtpSalesDate);
            groupBox1.Controls.Add(lblStatusBadge);
            groupBox1.Controls.Add(txtSalesNo);
            groupBox1.Location = new Point(0, 0);
            groupBox1.MinimumSize = new Size(880, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(880, 320);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "銷貨單主檔";
            // 
            // lblAuditTrail
            // 
            lblAuditTrail.AutoSize = true;
            lblAuditTrail.Font = new Font("微軟正黑體", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 136);
            lblAuditTrail.ForeColor = Color.Gray;
            lblAuditTrail.Location = new Point(19, 283);
            lblAuditTrail.Name = "lblAuditTrail";
            lblAuditTrail.Size = new Size(80, 17);
            lblAuditTrail.TabIndex = 25;
            lblAuditTrail.Text = "lblAuditTrail";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(368, 85);
            label9.Name = "label9";
            label9.Size = new Size(73, 20);
            label9.TabIndex = 24;
            label9.Text = "客戶名稱";
            // 
            // cmbDistrict
            // 
            cmbDistrict.BackColor = Color.LightBlue;
            cmbDistrict.Enabled = false;
            cmbDistrict.FormattingEnabled = true;
            cmbDistrict.Location = new Point(220, 127);
            cmbDistrict.Name = "cmbDistrict";
            cmbDistrict.Size = new Size(87, 28);
            cmbDistrict.TabIndex = 23;
            // 
            // cmbCity
            // 
            cmbCity.BackColor = Color.LightBlue;
            cmbCity.Enabled = false;
            cmbCity.FormattingEnabled = true;
            cmbCity.Location = new Point(113, 127);
            cmbCity.Name = "cmbCity";
            cmbCity.Size = new Size(87, 28);
            cmbCity.TabIndex = 22;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(19, 130);
            label8.Name = "label8";
            label8.Size = new Size(73, 20);
            label8.TabIndex = 21;
            label8.Text = "行政區域";
            // 
            // txtCustomerName
            // 
            txtCustomerName.Location = new Point(486, 82);
            txtCustomerName.Name = "txtCustomerName";
            txtCustomerName.ReadOnly = true;
            txtCustomerName.Size = new Size(388, 29);
            txtCustomerName.TabIndex = 20;
            // 
            // btnLookupCustomer
            // 
            btnLookupCustomer.AutoSize = true;
            btnLookupCustomer.Location = new Point(272, 81);
            btnLookupCustomer.Name = "btnLookupCustomer";
            btnLookupCustomer.Size = new Size(35, 30);
            btnLookupCustomer.TabIndex = 19;
            btnLookupCustomer.Text = "🔍";
            btnLookupCustomer.UseVisualStyleBackColor = true;
            // 
            // txtCustomerNo
            // 
            txtCustomerNo.BackColor = Color.LightBlue;
            txtCustomerNo.Location = new Point(113, 82);
            txtCustomerNo.Name = "txtCustomerNo";
            txtCustomerNo.ReadOnly = true;
            txtCustomerNo.Size = new Size(153, 29);
            txtCustomerNo.TabIndex = 18;
            // 
            // txtRemark
            // 
            txtRemark.Location = new Point(113, 219);
            txtRemark.Multiline = true;
            txtRemark.Name = "txtRemark";
            txtRemark.ReadOnly = true;
            txtRemark.Size = new Size(761, 58);
            txtRemark.TabIndex = 16;
            // 
            // txtShipAddress
            // 
            txtShipAddress.BackColor = Color.LightBlue;
            txtShipAddress.Location = new Point(113, 173);
            txtShipAddress.Name = "txtShipAddress";
            txtShipAddress.ReadOnly = true;
            txtShipAddress.Size = new Size(761, 29);
            txtShipAddress.TabIndex = 15;
            // 
            // txtShipZipRear
            // 
            txtShipZipRear.Location = new Point(532, 127);
            txtShipZipRear.MaxLength = 3;
            txtShipZipRear.Name = "txtShipZipRear";
            txtShipZipRear.ReadOnly = true;
            txtShipZipRear.Size = new Size(40, 29);
            txtShipZipRear.TabIndex = 14;
            // 
            // txtShipZipFront
            // 
            txtShipZipFront.Location = new Point(486, 127);
            txtShipZipFront.MaxLength = 3;
            txtShipZipFront.Name = "txtShipZipFront";
            txtShipZipFront.ReadOnly = true;
            txtShipZipFront.Size = new Size(40, 29);
            txtShipZipFront.TabIndex = 13;
            // 
            // lblTotalAmount
            // 
            lblTotalAmount.ForeColor = Color.Blue;
            lblTotalAmount.Location = new Point(608, 280);
            lblTotalAmount.Name = "lblTotalAmount";
            lblTotalAmount.Size = new Size(267, 20);
            lblTotalAmount.TabIndex = 11;
            lblTotalAmount.Text = "總計：$0";
            lblTotalAmount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(19, 222);
            label7.Name = "label7";
            label7.Size = new Size(73, 20);
            label7.TabIndex = 9;
            label7.Text = "單據備註";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(19, 176);
            label6.Name = "label6";
            label6.Size = new Size(73, 20);
            label6.TabIndex = 8;
            label6.Text = "出貨地址";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(368, 130);
            label5.Name = "label5";
            label5.Size = new Size(105, 20);
            label5.TabIndex = 7;
            label5.Text = "出貨郵遞區號";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(19, 85);
            label4.Name = "label4";
            label4.Size = new Size(73, 20);
            label4.TabIndex = 6;
            label4.Text = "客戶編號";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(368, 40);
            label3.Name = "label3";
            label3.Size = new Size(73, 20);
            label3.TabIndex = 5;
            label3.Text = "銷貨日期";
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
            // dtpSalesDate
            // 
            dtpSalesDate.Enabled = false;
            dtpSalesDate.Format = DateTimePickerFormat.Short;
            dtpSalesDate.Location = new Point(486, 34);
            dtpSalesDate.Name = "dtpSalesDate";
            dtpSalesDate.Size = new Size(126, 29);
            dtpSalesDate.TabIndex = 2;
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
            // txtSalesNo
            // 
            txtSalesNo.Location = new Point(113, 37);
            txtSalesNo.Name = "txtSalesNo";
            txtSalesNo.ReadOnly = true;
            txtSalesNo.Size = new Size(194, 29);
            txtSalesNo.TabIndex = 0;
            // 
            // dgvSalesDetail
            // 
            dgvSalesDetail.AllowUserToResizeRows = false;
            dgvSalesDetail.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSalesDetail.Dock = DockStyle.Fill;
            dgvSalesDetail.Location = new Point(0, 25);
            dgvSalesDetail.Name = "dgvSalesDetail";
            dgvSalesDetail.Size = new Size(880, 313);
            dgvSalesDetail.TabIndex = 0;
            // 
            // tsDetailActions
            // 
            tsDetailActions.Items.AddRange(new ToolStripItem[] { btnMoveUp, btnMoveDown });
            tsDetailActions.Location = new Point(0, 0);
            tsDetailActions.Name = "tsDetailActions";
            tsDetailActions.Size = new Size(880, 25);
            tsDetailActions.TabIndex = 1;
            tsDetailActions.Text = "toolStrip1";
            // 
            // btnMoveUp
            // 
            btnMoveUp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnMoveUp.Enabled = false;
            btnMoveUp.Image = (Image)resources.GetObject("btnMoveUp.Image");
            btnMoveUp.ImageTransparentColor = Color.Magenta;
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(47, 22);
            btnMoveUp.Text = "▲上移";
            // 
            // btnMoveDown
            // 
            btnMoveDown.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnMoveDown.Enabled = false;
            btnMoveDown.Image = (Image)resources.GetObject("btnMoveDown.Image");
            btnMoveDown.ImageTransparentColor = Color.Magenta;
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(47, 22);
            btnMoveDown.Text = "▼下移";
            // 
            // SalesOrderPage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitMain);
            Controls.Add(tsActions);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "SalesOrderPage";
            Size = new Size(1417, 697);
            tsActions.ResumeLayout(false);
            tsActions.PerformLayout();
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSalesMaster).EndInit();
            pnlSearch.ResumeLayout(false);
            pnlSearch.PerformLayout();
            splitRight.Panel1.ResumeLayout(false);
            splitRight.Panel2.ResumeLayout(false);
            splitRight.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitRight).EndInit();
            splitRight.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSalesDetail).EndInit();
            tsDetailActions.ResumeLayout(false);
            tsDetailActions.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip tsActions;
        private ToolStripButton btnAdd;
        private ToolStripButton btnEdit;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnPost;
        private ToolStripButton btnCancel;
        private ToolStripButton btnRefresh;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton btnVoid;
        private ToolStripButton btnSave;
        private SplitContainer splitMain;
        private Panel pnlSearch;
        private TextBox txtKeyword;
        private DataGridView dgvSalesMaster;
        private CheckBox chkShowVoided;
        private Button btnSearch;
        private SplitContainer splitRight;
        private GroupBox groupBox1;
        private DateTimePicker dtpSalesDate;
        private Label lblStatusBadge;
        private TextBox txtSalesNo;
        private Label label7;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private Label lblTotalAmount;
        private TextBox txtRemark;
        private TextBox txtShipAddress;
        private TextBox txtShipZipRear;
        private TextBox txtShipZipFront;
        private DataGridView dgvSalesDetail;
        private TextBox txtCustomerNo;
        private TextBox txtCustomerName;
        private Button btnLookupCustomer;
        private ComboBox cmbDistrict;
        private ComboBox cmbCity;
        private Label label8;
        private Label label9;
        private Core.PaginationControl ucPagination;
        private Label lblAuditTrail;
        private ToolStrip tsDetailActions;
        private ToolStripButton btnMoveUp;
        private ToolStripButton btnMoveDown;
    }
}
