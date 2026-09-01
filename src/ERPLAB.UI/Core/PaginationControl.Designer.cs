namespace ERPLAB.UI.Core
{
    partial class PaginationControl
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
            txtCurrentPage = new TextBox();
            cmbPageSize = new ComboBox();
            lblPageInfo = new Label();
            btnLastPage = new Button();
            btnNextPage = new Button();
            btnPrevPage = new Button();
            btnFirstPage = new Button();
            SuspendLayout();
            // 
            // txtCurrentPage
            // 
            txtCurrentPage.Location = new Point(139, 6);
            txtCurrentPage.Name = "txtCurrentPage";
            txtCurrentPage.Size = new Size(40, 29);
            txtCurrentPage.TabIndex = 13;
            txtCurrentPage.TextAlign = HorizontalAlignment.Center;
            // 
            // cmbPageSize
            // 
            cmbPageSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPageSize.FormattingEnabled = true;
            cmbPageSize.Location = new Point(461, 6);
            cmbPageSize.Name = "cmbPageSize";
            cmbPageSize.Size = new Size(69, 28);
            cmbPageSize.TabIndex = 12;
            // 
            // lblPageInfo
            // 
            lblPageInfo.AutoSize = true;
            lblPageInfo.Location = new Point(185, 10);
            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.Size = new Size(95, 20);
            lblPageInfo.TabIndex = 11;
            lblPageInfo.Text = "lblPageInfo";
            // 
            // btnLastPage
            // 
            btnLastPage.AutoSize = true;
            btnLastPage.Location = new Point(399, 5);
            btnLastPage.Name = "btnLastPage";
            btnLastPage.Size = new Size(60, 30);
            btnLastPage.TabIndex = 10;
            btnLastPage.Text = "▶|";
            btnLastPage.UseVisualStyleBackColor = true;
            // 
            // btnNextPage
            // 
            btnNextPage.AutoSize = true;
            btnNextPage.Location = new Point(333, 5);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new Size(60, 30);
            btnNextPage.TabIndex = 9;
            btnNextPage.Text = "▶";
            btnNextPage.UseVisualStyleBackColor = true;
            // 
            // btnPrevPage
            // 
            btnPrevPage.AutoSize = true;
            btnPrevPage.Location = new Point(69, 5);
            btnPrevPage.Name = "btnPrevPage";
            btnPrevPage.Size = new Size(60, 30);
            btnPrevPage.TabIndex = 8;
            btnPrevPage.Text = "◀";
            btnPrevPage.UseVisualStyleBackColor = true;
            // 
            // btnFirstPage
            // 
            btnFirstPage.AutoSize = true;
            btnFirstPage.Location = new Point(3, 5);
            btnFirstPage.Name = "btnFirstPage";
            btnFirstPage.Size = new Size(60, 30);
            btnFirstPage.TabIndex = 7;
            btnFirstPage.Text = "|◀";
            btnFirstPage.UseVisualStyleBackColor = true;
            // 
            // PaginationControl
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(txtCurrentPage);
            Controls.Add(cmbPageSize);
            Controls.Add(lblPageInfo);
            Controls.Add(btnLastPage);
            Controls.Add(btnNextPage);
            Controls.Add(btnPrevPage);
            Controls.Add(btnFirstPage);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "PaginationControl";
            Size = new Size(534, 40);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtCurrentPage;
        private ComboBox cmbPageSize;
        private Label lblPageInfo;
        private Button btnLastPage;
        private Button btnNextPage;
        private Button btnPrevPage;
        private Button btnFirstPage;
    }
}
