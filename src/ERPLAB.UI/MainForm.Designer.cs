namespace ERPLAB.UI
{
    partial class MainForm
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
            splitContainerMain = new SplitContainer();
            flpMenu = new FlowLayoutPanel();
            tabControlMain = new TabControl();
            pnlTop = new Panel();
            label1 = new Label();
            btnLogout = new Button();
            statusStripMain = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripStatusLabel2 = new ToolStripStatusLabel();
            toolStripStatusLabel3 = new ToolStripStatusLabel();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            pnlTop.SuspendLayout();
            statusStripMain.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel1;
            splitContainerMain.IsSplitterFixed = true;
            splitContainerMain.Location = new Point(0, 50);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.AutoScroll = true;
            splitContainerMain.Panel1.Controls.Add(flpMenu);
            splitContainerMain.Panel1MinSize = 210;
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(tabControlMain);
            splitContainerMain.Panel2MinSize = 794;
            splitContainerMain.Size = new Size(1264, 609);
            splitContainerMain.SplitterDistance = 210;
            splitContainerMain.TabIndex = 0;
            // 
            // flpMenu
            // 
            flpMenu.AutoScroll = true;
            flpMenu.Dock = DockStyle.Fill;
            flpMenu.FlowDirection = FlowDirection.TopDown;
            flpMenu.Location = new Point(0, 0);
            flpMenu.Name = "flpMenu";
            flpMenu.Size = new Size(210, 609);
            flpMenu.TabIndex = 0;
            flpMenu.WrapContents = false;
            // 
            // tabControlMain
            // 
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.ItemSize = new Size(120, 30);
            tabControlMain.Location = new Point(0, 0);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1050, 609);
            tabControlMain.TabIndex = 0;
            // 
            // pnlTop
            // 
            pnlTop.BackColor = Color.DarkGray;
            pnlTop.Controls.Add(label1);
            pnlTop.Controls.Add(btnLogout);
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Location = new Point(0, 0);
            pnlTop.Name = "pnlTop";
            pnlTop.Size = new Size(1264, 50);
            pnlTop.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Left;
            label1.Font = new Font("等距更紗黑體 TC", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 136);
            label1.ForeColor = Color.White;
            label1.Location = new Point(0, 0);
            label1.Name = "label1";
            label1.Size = new Size(78, 26);
            label1.TabIndex = 1;
            label1.Text = "ERPLAB";
            // 
            // btnLogout
            // 
            btnLogout.Dock = DockStyle.Right;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(1164, 0);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(100, 50);
            btnLogout.TabIndex = 0;
            btnLogout.Text = "登出";
            btnLogout.UseVisualStyleBackColor = true;
            btnLogout.Click += btnLogout_Click;
            // 
            // statusStripMain
            // 
            statusStripMain.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2, toolStripStatusLabel3 });
            statusStripMain.Location = new Point(0, 659);
            statusStripMain.Name = "statusStripMain";
            statusStripMain.Size = new Size(1264, 22);
            statusStripMain.TabIndex = 2;
            statusStripMain.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(67, 17);
            toolStripStatusLabel1.Text = "狀態：就緒";
            // 
            // toolStripStatusLabel2
            // 
            toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            toolStripStatusLabel2.Size = new Size(1106, 17);
            toolStripStatusLabel2.Spring = true;
            // 
            // toolStripStatusLabel3
            // 
            toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            toolStripStatusLabel3.Size = new Size(76, 17);
            toolStripStatusLabel3.Text = "版本：v1.0.0";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1264, 681);
            Controls.Add(splitContainerMain);
            Controls.Add(statusStripMain);
            Controls.Add(pnlTop);
            Font = new Font("等距更紗黑體 TC", 12F, FontStyle.Regular, GraphicsUnit.Point, 136);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(1280, 720);
            Name = "MainForm";
            Text = "MainForm";
            Load += MainForm_Load;
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            pnlTop.ResumeLayout(false);
            pnlTop.PerformLayout();
            statusStripMain.ResumeLayout(false);
            statusStripMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SplitContainer splitContainerMain;
        private TabControl tabControlMain;
        private FlowLayoutPanel flpMenu;
        private Panel pnlTop;
        private Button btnLogout;
        private Label label1;
        private StatusStrip statusStripMain;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
    }
}