namespace ERPLAB.UI.Views.BaseData
{
    partial class CustomerLookupForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSearch = new Button();
            txtKeyword = new TextBox();
            dgvList = new DataGridView();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvList).BeginInit();
            SuspendLayout();
            // 
            // btnSearch
            // 
            btnSearch.AutoSize = true;
            btnSearch.Location = new Point(438, 84);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(80, 30);
            btnSearch.TabIndex = 0;
            btnSearch.Text = "🔍 搜尋";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // txtKeyword
            // 
            txtKeyword.Location = new Point(177, 85);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(236, 29);
            txtKeyword.TabIndex = 1;
            // 
            // dgvList
            // 
            dgvList.AllowUserToAddRows = false;
            dgvList.AllowUserToDeleteRows = false;
            dgvList.AllowUserToResizeRows = false;
            dgvList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvList.Location = new Point(12, 196);
            dgvList.Name = "dgvList";
            dgvList.ReadOnly = true;
            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.Size = new Size(721, 279);
            dgvList.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(177, 131);
            label1.Name = "label1";
            label1.Size = new Size(341, 20);
            label1.TabIndex = 3;
            label1.Text = "可依客戶編號,客戶名稱,電話號碼,統一編號搜尋";
            // 
            // CustomerLookupForm
            // 
            AutoScaleDimensions = new SizeF(10F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(747, 486);
            Controls.Add(label1);
            Controls.Add(dgvList);
            Controls.Add(txtKeyword);
            Controls.Add(btnSearch);
            Font = new Font("微軟正黑體", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(4);
            Name = "CustomerLookupForm";
            Text = "CustomerLookupForm";
            Load += CustomerLookupForm_Load;
            ((System.ComponentModel.ISupportInitialize)dgvList).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSearch;
        private TextBox txtKeyword;
        private DataGridView dgvList;
        private Label label1;
    }
}