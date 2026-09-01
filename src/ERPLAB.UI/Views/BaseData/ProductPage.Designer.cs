namespace ERPLAB.UI.Views.BaseData
{
    partial class ProductPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProductPage));
            tsActions = new ToolStrip();
            btnRefresh = new ToolStripButton();
            btnToggleStatus = new ToolStripButton();
            btnCancel = new ToolStripButton();
            btnSave = new ToolStripButton();
            btnEdit = new ToolStripButton();
            btnAdd = new ToolStripButton();
            splitContainerMain = new SplitContainer();
            dgvProducts = new DataGridView();
            ucPagination = new ERPLAB.UI.Core.PaginationControl();
            pnlSearch = new Panel();
            btnSearch = new Button();
            txtKeyword = new TextBox();
            chkShowInactive = new CheckBox();
            panel1 = new Panel();
            txtCurrentStock = new TextBox();
            txtDescription = new TextBox();
            txtSalesPrice = new TextBox();
            txtRemark = new TextBox();
            txtPurchasePrice = new TextBox();
            txtProductName = new TextBox();
            txtProductNo = new TextBox();
            lblStatusBadge = new Label();
            label9 = new Label();
            label8 = new Label();
            lblSalesPrice = new Label();
            label4 = new Label();
            lblPurchasePrice = new Label();
            label2 = new Label();
            label1 = new Label();
            lblAuditTrail = new Label();
            lblMovingAverageCost = new Label();
            txtMovingAverageCost = new TextBox();
            tsActions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvProducts).BeginInit();
            pnlSearch.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tsActions
            // 
            tsActions.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            tsActions.GripStyle = ToolStripGripStyle.Hidden;
            tsActions.ImageScalingSize = new Size(28, 28);
            tsActions.Items.AddRange(new ToolStripItem[] { btnRefresh, btnToggleStatus, btnCancel, btnSave, btnEdit, btnAdd });
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
            // btnToggleStatus
            // 
            btnToggleStatus.Alignment = ToolStripItemAlignment.Right;
            btnToggleStatus.Image = (Image)resources.GetObject("btnToggleStatus.Image");
            btnToggleStatus.ImageTransparentColor = Color.Magenta;
            btnToggleStatus.Name = "btnToggleStatus";
            btnToggleStatus.Size = new Size(73, 32);
            btnToggleStatus.Text = "停用";
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
            splitContainerMain.Panel1.Controls.Add(dgvProducts);
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
            splitContainerMain.Size = new Size(1073, 568);
            splitContainerMain.SplitterDistance = 380;
            splitContainerMain.SplitterIncrement = 5;
            splitContainerMain.SplitterWidth = 6;
            splitContainerMain.TabIndex = 1;
            // 
            // dgvProducts
            // 
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.AllowUserToDeleteRows = false;
            dgvProducts.AllowUserToResizeRows = false;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvProducts.BackgroundColor = Color.White;
            dgvProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvProducts.Dock = DockStyle.Fill;
            dgvProducts.Location = new Point(0, 50);
            dgvProducts.MultiSelect = false;
            dgvProducts.Name = "dgvProducts";
            dgvProducts.ReadOnly = true;
            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.Size = new Size(380, 480);
            dgvProducts.TabIndex = 0;
            // 
            // ucPagination
            // 
            ucPagination.Dock = DockStyle.Bottom;
            ucPagination.Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            ucPagination.Location = new Point(0, 530);
            ucPagination.Margin = new Padding(4);
            ucPagination.Name = "ucPagination";
            ucPagination.Size = new Size(380, 38);
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
            pnlSearch.Size = new Size(380, 50);
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
            chkShowInactive.Text = "包含已停用商品";
            chkShowInactive.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(txtMovingAverageCost);
            panel1.Controls.Add(lblMovingAverageCost);
            panel1.Controls.Add(txtCurrentStock);
            panel1.Controls.Add(txtDescription);
            panel1.Controls.Add(txtSalesPrice);
            panel1.Controls.Add(txtRemark);
            panel1.Controls.Add(txtPurchasePrice);
            panel1.Controls.Add(txtProductName);
            panel1.Controls.Add(txtProductNo);
            panel1.Controls.Add(lblStatusBadge);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(label8);
            panel1.Controls.Add(lblSalesPrice);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(lblPurchasePrice);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(lblAuditTrail);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(660, 565);
            panel1.TabIndex = 24;
            // 
            // txtCurrentStock
            // 
            txtCurrentStock.Location = new Point(151, 257);
            txtCurrentStock.Name = "txtCurrentStock";
            txtCurrentStock.ReadOnly = true;
            txtCurrentStock.Size = new Size(139, 29);
            txtCurrentStock.TabIndex = 27;
            // 
            // txtDescription
            // 
            txtDescription.Location = new Point(151, 316);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.ReadOnly = true;
            txtDescription.Size = new Size(404, 87);
            txtDescription.TabIndex = 26;
            // 
            // txtSalesPrice
            // 
            txtSalesPrice.BackColor = Color.LightBlue;
            txtSalesPrice.Location = new Point(151, 198);
            txtSalesPrice.Name = "txtSalesPrice";
            txtSalesPrice.ReadOnly = true;
            txtSalesPrice.Size = new Size(139, 29);
            txtSalesPrice.TabIndex = 25;
            // 
            // txtRemark
            // 
            txtRemark.Location = new Point(151, 433);
            txtRemark.Multiline = true;
            txtRemark.Name = "txtRemark";
            txtRemark.ReadOnly = true;
            txtRemark.Size = new Size(404, 87);
            txtRemark.TabIndex = 17;
            // 
            // txtPurchasePrice
            // 
            txtPurchasePrice.BackColor = Color.LightBlue;
            txtPurchasePrice.Location = new Point(151, 139);
            txtPurchasePrice.MaxLength = 8;
            txtPurchasePrice.Name = "txtPurchasePrice";
            txtPurchasePrice.ReadOnly = true;
            txtPurchasePrice.Size = new Size(139, 29);
            txtPurchasePrice.TabIndex = 12;
            // 
            // txtProductName
            // 
            txtProductName.BackColor = Color.LightBlue;
            txtProductName.Location = new Point(151, 80);
            txtProductName.MaxLength = 50;
            txtProductName.Name = "txtProductName";
            txtProductName.ReadOnly = true;
            txtProductName.Size = new Size(404, 29);
            txtProductName.TabIndex = 11;
            // 
            // txtProductNo
            // 
            txtProductNo.BackColor = SystemColors.Control;
            txtProductNo.Location = new Point(151, 21);
            txtProductNo.MaxLength = 20;
            txtProductNo.Name = "txtProductNo";
            txtProductNo.ReadOnly = true;
            txtProductNo.Size = new Size(139, 29);
            txtProductNo.TabIndex = 10;
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
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(24, 438);
            label9.Name = "label9";
            label9.Size = new Size(73, 20);
            label9.TabIndex = 8;
            label9.Text = "備註說明";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(24, 260);
            label8.Name = "label8";
            label8.Size = new Size(89, 20);
            label8.TabIndex = 7;
            label8.Text = "帳面庫存量";
            // 
            // lblSalesPrice
            // 
            lblSalesPrice.AutoSize = true;
            lblSalesPrice.Location = new Point(24, 201);
            lblSalesPrice.Name = "lblSalesPrice";
            lblSalesPrice.Size = new Size(73, 20);
            lblSalesPrice.TabIndex = 4;
            lblSalesPrice.Text = "零售售價";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(24, 319);
            label4.Name = "label4";
            label4.Size = new Size(112, 20);
            label4.TabIndex = 3;
            label4.Text = "商品規格/介紹";
            // 
            // lblPurchasePrice
            // 
            lblPurchasePrice.AutoSize = true;
            lblPurchasePrice.Location = new Point(24, 142);
            lblPurchasePrice.Name = "lblPurchasePrice";
            lblPurchasePrice.Size = new Size(105, 20);
            lblPurchasePrice.TabIndex = 2;
            lblPurchasePrice.Text = "參考進貨單價";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 83);
            label2.Name = "label2";
            label2.Size = new Size(73, 20);
            label2.TabIndex = 1;
            label2.Text = "商品名稱";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 24);
            label1.Name = "label1";
            label1.Size = new Size(73, 20);
            label1.TabIndex = 0;
            label1.Text = "商品編號";
            // 
            // lblAuditTrail
            // 
            lblAuditTrail.Font = new Font("微軟正黑體", 9F, FontStyle.Regular, GraphicsUnit.Point, 136);
            lblAuditTrail.ForeColor = Color.Gray;
            lblAuditTrail.Location = new Point(3, 546);
            lblAuditTrail.Margin = new Padding(3);
            lblAuditTrail.Name = "lblAuditTrail";
            lblAuditTrail.Size = new Size(654, 16);
            lblAuditTrail.TabIndex = 23;
            lblAuditTrail.Text = "lblAuditTrail";
            lblAuditTrail.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblMovingAverageCost
            // 
            lblMovingAverageCost.AutoSize = true;
            lblMovingAverageCost.Location = new Point(334, 144);
            lblMovingAverageCost.Name = "lblMovingAverageCost";
            lblMovingAverageCost.Size = new Size(73, 20);
            lblMovingAverageCost.TabIndex = 28;
            lblMovingAverageCost.Text = "平均成本";
            // 
            // txtMovingAverageCost
            // 
            txtMovingAverageCost.Location = new Point(416, 139);
            txtMovingAverageCost.Name = "txtMovingAverageCost";
            txtMovingAverageCost.ReadOnly = true;
            txtMovingAverageCost.Size = new Size(139, 29);
            txtMovingAverageCost.TabIndex = 29;
            // 
            // ProductPage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            Controls.Add(splitContainerMain);
            Controls.Add(tsActions);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            MinimumSize = new Size(760, 400);
            Name = "ProductPage";
            Size = new Size(1073, 603);
            tsActions.ResumeLayout(false);
            tsActions.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvProducts).EndInit();
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
        private ToolStripButton btnToggleStatus;
        private ToolStripButton btnRefresh;
        private SplitContainer splitContainerMain;
        private DataGridView dgvProducts;
        private Panel pnlSearch;
        private CheckBox chkShowInactive;
        private Button btnSearch;
        private TextBox txtKeyword;
        private Core.PaginationControl ucPagination;
        private TextBox txtRemark;
        private TextBox txtPurchasePrice;
        private TextBox txtProductName;
        private TextBox txtProductNo;
        private Label lblStatusBadge;
        private Label label9;
        private Label label8;
        private Label lblSalesPrice;
        private Label label4;
        private Label lblPurchasePrice;
        private Label label2;
        private Label label1;
        private Label lblAuditTrail;
        private Panel panel1;
        private TextBox txtSalesPrice;
        private TextBox txtCurrentStock;
        private TextBox txtDescription;
        private Label lblMovingAverageCost;
        private TextBox txtMovingAverageCost;
    }
}
