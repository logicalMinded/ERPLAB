namespace ERPLAB.UI.Views.BaseData
{
    partial class EmployeeLookupForm
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
            label1 = new Label();
            dgvList = new DataGridView();
            txtKeyword = new TextBox();
            btnSearch = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvList).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(179, 130);
            label1.Name = "label1";
            label1.Size = new Size(205, 20);
            label1.TabIndex = 7;
            label1.Text = "可依員工編號,員工名稱搜尋";
            // 
            // dgvList
            // 
            dgvList.AllowUserToAddRows = false;
            dgvList.AllowUserToDeleteRows = false;
            dgvList.AllowUserToResizeRows = false;
            dgvList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvList.Location = new Point(14, 195);
            dgvList.Name = "dgvList";
            dgvList.ReadOnly = true;
            dgvList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvList.Size = new Size(721, 279);
            dgvList.TabIndex = 6;
            // 
            // txtKeyword
            // 
            txtKeyword.Location = new Point(179, 84);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(226, 29);
            txtKeyword.TabIndex = 5;
            // 
            // btnSearch
            // 
            btnSearch.AutoSize = true;
            btnSearch.Location = new Point(440, 82);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(80, 30);
            btnSearch.TabIndex = 4;
            btnSearch.Text = "🔍 搜尋";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // EmployeeLookupForm
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
            Name = "EmployeeLookupForm";
            Text = "EmployeeLookupForm";
            Load += EmployeeLookupForm_Load;
            ((System.ComponentModel.ISupportInitialize)dgvList).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private DataGridView dgvList;
        private TextBox txtKeyword;
        private Button btnSearch;
    }
}