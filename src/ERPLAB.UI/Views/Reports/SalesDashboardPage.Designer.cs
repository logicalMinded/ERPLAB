namespace ERPLAB.UI.Views.Reports
{
    partial class SalesDashboardPage
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) { components.Dispose(); }
            base.Dispose(disposing);
        }

        #region 元件設計工具產生的程式碼

        private void InitializeComponent()
        {
            pnlFilter = new Panel();
            btnSearch = new Button();
            btnThisYear = new Button();
            btnThisMonth = new Button();
            dtpEndDate = new DateTimePicker();
            label2 = new Label();
            dtpStartDate = new DateTimePicker();
            label1 = new Label();
            tlpKPIs = new TableLayoutPanel();
            pnlKpi4 = new Panel();
            lblAovValue = new Label();
            label10 = new Label();
            pnlKpi3 = new Panel();
            lblOrdersValue = new Label();
            label8 = new Label();
            pnlKpi2 = new Panel();
            lblGrossProfitValue = new Label();
            label6 = new Label();
            pnlKpi1 = new Panel();
            lblRevenueValue = new Label();
            label3 = new Label();
            splitContainerMain = new SplitContainer();
            dgvTopProducts = new DataGridView();
            label11 = new Label();
            dgvTopCustomers = new DataGridView();
            label12 = new Label();
            pnlFilter.SuspendLayout();
            tlpKPIs.SuspendLayout();
            pnlKpi4.SuspendLayout();
            pnlKpi3.SuspendLayout();
            pnlKpi2.SuspendLayout();
            pnlKpi1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTopProducts).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTopCustomers).BeginInit();
            SuspendLayout();
            // 
            // pnlFilter
            // 
            pnlFilter.Controls.Add(btnSearch);
            pnlFilter.Controls.Add(btnThisYear);
            pnlFilter.Controls.Add(btnThisMonth);
            pnlFilter.Controls.Add(dtpEndDate);
            pnlFilter.Controls.Add(label2);
            pnlFilter.Controls.Add(dtpStartDate);
            pnlFilter.Controls.Add(label1);
            pnlFilter.Dock = DockStyle.Top;
            pnlFilter.Location = new Point(0, 0);
            pnlFilter.Name = "pnlFilter";
            pnlFilter.Size = new Size(1000, 60);
            pnlFilter.TabIndex = 0;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = Color.SteelBlue;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.ForeColor = Color.White;
            btnSearch.Location = new Point(380, 15);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(91, 30);
            btnSearch.TabIndex = 6;
            btnSearch.Text = "產生報表";
            btnSearch.UseVisualStyleBackColor = false;
            // 
            // btnThisYear
            // 
            btnThisYear.Location = new Point(566, 15);
            btnThisYear.Name = "btnThisYear";
            btnThisYear.Size = new Size(70, 30);
            btnThisYear.TabIndex = 5;
            btnThisYear.Text = "本年";
            btnThisYear.UseVisualStyleBackColor = true;
            // 
            // btnThisMonth
            // 
            btnThisMonth.Location = new Point(486, 15);
            btnThisMonth.Name = "btnThisMonth";
            btnThisMonth.Size = new Size(70, 30);
            btnThisMonth.TabIndex = 4;
            btnThisMonth.Text = "本月";
            btnThisMonth.UseVisualStyleBackColor = true;
            // 
            // dtpEndDate
            // 
            dtpEndDate.Format = DateTimePickerFormat.Short;
            dtpEndDate.Location = new Point(240, 15);
            dtpEndDate.Name = "dtpEndDate";
            dtpEndDate.Size = new Size(120, 29);
            dtpEndDate.TabIndex = 3;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(210, 20);
            label2.Name = "label2";
            label2.Size = new Size(25, 20);
            label2.TabIndex = 2;
            label2.Text = "至";
            // 
            // dtpStartDate
            // 
            dtpStartDate.Format = DateTimePickerFormat.Short;
            dtpStartDate.Location = new Point(80, 15);
            dtpStartDate.Name = "dtpStartDate";
            dtpStartDate.Size = new Size(120, 29);
            dtpStartDate.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(10, 20);
            label1.Name = "label1";
            label1.Size = new Size(73, 20);
            label1.TabIndex = 0;
            label1.Text = "統計區間";
            // 
            // tlpKPIs
            // 
            tlpKPIs.ColumnCount = 4;
            tlpKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            tlpKPIs.Controls.Add(pnlKpi4, 3, 0);
            tlpKPIs.Controls.Add(pnlKpi3, 2, 0);
            tlpKPIs.Controls.Add(pnlKpi2, 1, 0);
            tlpKPIs.Controls.Add(pnlKpi1, 0, 0);
            tlpKPIs.Dock = DockStyle.Top;
            tlpKPIs.Location = new Point(0, 60);
            tlpKPIs.Name = "tlpKPIs";
            tlpKPIs.RowCount = 1;
            tlpKPIs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpKPIs.Size = new Size(1000, 120);
            tlpKPIs.TabIndex = 1;
            // 
            // pnlKpi4
            // 
            pnlKpi4.BackColor = Color.White;
            pnlKpi4.BorderStyle = BorderStyle.FixedSingle;
            pnlKpi4.Controls.Add(lblAovValue);
            pnlKpi4.Controls.Add(label10);
            pnlKpi4.Dock = DockStyle.Fill;
            pnlKpi4.Location = new Point(753, 3);
            pnlKpi4.Name = "pnlKpi4";
            pnlKpi4.Size = new Size(244, 114);
            pnlKpi4.TabIndex = 3;
            // 
            // lblAovValue
            // 
            lblAovValue.Dock = DockStyle.Fill;
            lblAovValue.Font = new Font("Arial", 20F, FontStyle.Bold);
            lblAovValue.ForeColor = Color.Purple;
            lblAovValue.Location = new Point(0, 30);
            lblAovValue.Name = "lblAovValue";
            lblAovValue.Size = new Size(242, 82);
            lblAovValue.TabIndex = 1;
            lblAovValue.Text = "$0";
            lblAovValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.Dock = DockStyle.Top;
            label10.Font = new Font("微軟正黑體", 12F, FontStyle.Bold);
            label10.ForeColor = Color.DimGray;
            label10.Location = new Point(0, 0);
            label10.Name = "label10";
            label10.Size = new Size(242, 30);
            label10.TabIndex = 0;
            label10.Text = "平均客單價 (ATV)";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlKpi3
            // 
            pnlKpi3.BackColor = Color.White;
            pnlKpi3.BorderStyle = BorderStyle.FixedSingle;
            pnlKpi3.Controls.Add(lblOrdersValue);
            pnlKpi3.Controls.Add(label8);
            pnlKpi3.Dock = DockStyle.Fill;
            pnlKpi3.Location = new Point(503, 3);
            pnlKpi3.Name = "pnlKpi3";
            pnlKpi3.Size = new Size(244, 114);
            pnlKpi3.TabIndex = 2;
            // 
            // lblOrdersValue
            // 
            lblOrdersValue.Dock = DockStyle.Fill;
            lblOrdersValue.Font = new Font("Arial", 20F, FontStyle.Bold);
            lblOrdersValue.ForeColor = Color.DarkOrange;
            lblOrdersValue.Location = new Point(0, 30);
            lblOrdersValue.Name = "lblOrdersValue";
            lblOrdersValue.Size = new Size(242, 82);
            lblOrdersValue.TabIndex = 1;
            lblOrdersValue.Text = "0 張";
            lblOrdersValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.Dock = DockStyle.Top;
            label8.Font = new Font("微軟正黑體", 12F, FontStyle.Bold);
            label8.ForeColor = Color.DimGray;
            label8.Location = new Point(0, 0);
            label8.Name = "label8";
            label8.Size = new Size(242, 30);
            label8.TabIndex = 0;
            label8.Text = "有效訂單數";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlKpi2
            // 
            pnlKpi2.BackColor = Color.White;
            pnlKpi2.BorderStyle = BorderStyle.FixedSingle;
            pnlKpi2.Controls.Add(lblGrossProfitValue);
            pnlKpi2.Controls.Add(label6);
            pnlKpi2.Dock = DockStyle.Fill;
            pnlKpi2.Location = new Point(253, 3);
            pnlKpi2.Name = "pnlKpi2";
            pnlKpi2.Size = new Size(244, 114);
            pnlKpi2.TabIndex = 1;
            // 
            // lblGrossProfitValue
            // 
            lblGrossProfitValue.Dock = DockStyle.Fill;
            lblGrossProfitValue.Font = new Font("Arial", 20F, FontStyle.Bold);
            lblGrossProfitValue.ForeColor = Color.Green;
            lblGrossProfitValue.Location = new Point(0, 30);
            lblGrossProfitValue.Name = "lblGrossProfitValue";
            lblGrossProfitValue.Size = new Size(242, 82);
            lblGrossProfitValue.TabIndex = 1;
            lblGrossProfitValue.Text = "$0 (0%)";
            lblGrossProfitValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.Dock = DockStyle.Top;
            label6.Font = new Font("微軟正黑體", 12F, FontStyle.Bold);
            label6.ForeColor = Color.DimGray;
            label6.Location = new Point(0, 0);
            label6.Name = "label6";
            label6.Size = new Size(242, 30);
            label6.TabIndex = 0;
            label6.Text = "銷貨總毛利 (毛利率)";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlKpi1
            // 
            pnlKpi1.BackColor = Color.White;
            pnlKpi1.BorderStyle = BorderStyle.FixedSingle;
            pnlKpi1.Controls.Add(lblRevenueValue);
            pnlKpi1.Controls.Add(label3);
            pnlKpi1.Dock = DockStyle.Fill;
            pnlKpi1.Location = new Point(3, 3);
            pnlKpi1.Name = "pnlKpi1";
            pnlKpi1.Size = new Size(244, 114);
            pnlKpi1.TabIndex = 0;
            // 
            // lblRevenueValue
            // 
            lblRevenueValue.Dock = DockStyle.Fill;
            lblRevenueValue.Font = new Font("Arial", 24F, FontStyle.Bold);
            lblRevenueValue.ForeColor = Color.RoyalBlue;
            lblRevenueValue.Location = new Point(0, 30);
            lblRevenueValue.Name = "lblRevenueValue";
            lblRevenueValue.Size = new Size(242, 82);
            lblRevenueValue.TabIndex = 1;
            lblRevenueValue.Text = "$0";
            lblRevenueValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.Dock = DockStyle.Top;
            label3.Font = new Font("微軟正黑體", 12F, FontStyle.Bold);
            label3.ForeColor = Color.DimGray;
            label3.Location = new Point(0, 0);
            label3.Name = "label3";
            label3.Size = new Size(242, 30);
            label3.TabIndex = 0;
            label3.Text = "有效總營業額";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Location = new Point(0, 180);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(dgvTopProducts);
            splitContainerMain.Panel1.Controls.Add(label11);
            splitContainerMain.Panel1.Padding = new Padding(10);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(dgvTopCustomers);
            splitContainerMain.Panel2.Controls.Add(label12);
            splitContainerMain.Panel2.Padding = new Padding(10);
            splitContainerMain.Size = new Size(1000, 420);
            splitContainerMain.SplitterDistance = 500;
            splitContainerMain.TabIndex = 2;
            // 
            // dgvTopProducts
            // 
            dgvTopProducts.AllowUserToAddRows = false;
            dgvTopProducts.AllowUserToDeleteRows = false;
            dgvTopProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTopProducts.BackgroundColor = Color.White;
            dgvTopProducts.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTopProducts.Dock = DockStyle.Fill;
            dgvTopProducts.Location = new Point(10, 40);
            dgvTopProducts.Name = "dgvTopProducts";
            dgvTopProducts.ReadOnly = true;
            dgvTopProducts.RowHeadersVisible = false;
            dgvTopProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTopProducts.Size = new Size(480, 370);
            dgvTopProducts.TabIndex = 1;
            // 
            // label11
            // 
            label11.Dock = DockStyle.Top;
            label11.Font = new Font("微軟正黑體", 14F, FontStyle.Bold);
            label11.ForeColor = Color.DarkSlateGray;
            label11.Location = new Point(10, 10);
            label11.Name = "label11";
            label11.Size = new Size(480, 30);
            label11.TabIndex = 0;
            label11.Text = "🔥 熱銷商品 Top 10 (按銷售量)";
            // 
            // dgvTopCustomers
            // 
            dgvTopCustomers.AllowUserToAddRows = false;
            dgvTopCustomers.AllowUserToDeleteRows = false;
            dgvTopCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTopCustomers.BackgroundColor = Color.White;
            dgvTopCustomers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTopCustomers.Dock = DockStyle.Fill;
            dgvTopCustomers.Location = new Point(10, 40);
            dgvTopCustomers.Name = "dgvTopCustomers";
            dgvTopCustomers.ReadOnly = true;
            dgvTopCustomers.RowHeadersVisible = false;
            dgvTopCustomers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTopCustomers.Size = new Size(476, 370);
            dgvTopCustomers.TabIndex = 2;
            // 
            // label12
            // 
            label12.Dock = DockStyle.Top;
            label12.Font = new Font("微軟正黑體", 14F, FontStyle.Bold);
            label12.ForeColor = Color.DarkSlateGray;
            label12.Location = new Point(10, 10);
            label12.Name = "label12";
            label12.Size = new Size(476, 30);
            label12.TabIndex = 1;
            label12.Text = "👑 VIP 客戶貢獻 Top 10 (按營業額)";
            // 
            // SalesDashboardPage
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainerMain);
            Controls.Add(tlpKPIs);
            Controls.Add(pnlFilter);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "SalesDashboardPage";
            Size = new Size(1000, 600);
            pnlFilter.ResumeLayout(false);
            pnlFilter.PerformLayout();
            tlpKPIs.ResumeLayout(false);
            pnlKpi4.ResumeLayout(false);
            pnlKpi3.ResumeLayout(false);
            pnlKpi2.ResumeLayout(false);
            pnlKpi1.ResumeLayout(false);
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvTopProducts).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTopCustomers).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpEndDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dtpStartDate;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.Button btnThisYear;
        private System.Windows.Forms.Button btnThisMonth;
        private System.Windows.Forms.TableLayoutPanel tlpKPIs;
        private System.Windows.Forms.Panel pnlKpi4;
        private System.Windows.Forms.Label lblAovValue;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Panel pnlKpi3;
        private System.Windows.Forms.Label lblOrdersValue;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel pnlKpi2;
        private System.Windows.Forms.Label lblGrossProfitValue;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel pnlKpi1;
        private System.Windows.Forms.Label lblRevenueValue;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private System.Windows.Forms.DataGridView dgvTopProducts;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.DataGridView dgvTopCustomers;
        private System.Windows.Forms.Label label12;
    }
}