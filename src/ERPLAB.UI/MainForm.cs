using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;

namespace ERPLAB.UI
{
    public partial class MainForm : Form
    {
        private readonly List<SystemNode> _authorizedNodes;
        public MainForm()
        {
            InitializeComponent();
            _authorizedNodes = SessionContext.AuthorizedNodes ?? new List<SystemNode>();
            // 💡 啟動 TabControl 的自繪模式，以實作「X」關閉按鈕
            tabControlMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControlMain.DrawItem += TabControlMain_DrawItem;
            tabControlMain.MouseDown += TabControlMain_MouseDown;
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            this.Text = $"ERPLAB 企業資源規劃系統 - 當前使用者：{SessionContext.Username}";

            BuildAccordionMenu();
            LoadDefaultDashboard();
        }

        // =====================================================================
        // 🏠 [基礎引擎] 預設儀表板與登出機制
        // =====================================================================
        private void LoadDefaultDashboard()
        {
            // 💡 權限動態判斷：若具備戰情室權限，直接實體化 Dashboard 並嵌入首頁
            if (SessionContext.HasPermission("PAGE_SALES_DASHBOARD"))
            {
                var dashboard = new Views.Reports.SalesDashboardPage();
                dashboard.Dock = DockStyle.Fill;

                TabPage homeTab = new TabPage("📊 銷售儀表板  ")
                {
                    Name = "ERPLAB.UI.Views.Reports.SalesDashboardPage"
                };
                homeTab.Controls.Add(dashboard);
                tabControlMain.TabPages.Add(homeTab);
            }
            else
            {
                // 若無權限，顯示一般基層員工的歡迎詞
                TabPage homeTab = new TabPage("🏠 系統首頁  ")
                {
                    Name = "HomeTab",
                    BackColor = Color.White
                };

                Label lblWelcome = new Label
                {
                    Text = $"歡迎回來，{SessionContext.Username}！\n請從左側選單選擇作業模組。",
                    AutoSize = true,
                    Font = new Font("微軟正黑體", 14),
                    Location = new Point(50, 50)
                };

                homeTab.Controls.Add(lblWelcome);
                tabControlMain.TabPages.Add(homeTab);
            }
        }
        private void btnLogout_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("確定要登出並切換使用者嗎？", "登出確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionContext.IsLogoutRequested = true;
                SessionContext.Logout();
                this.Close(); // 關閉主畫面，Program.cs 會接管並重啟 LoginForm
            }
        }

        // =====================================================================
        // 🍔 [建構引擎] N 層無限遞迴摺疊選單 (Recursive Accordion Menu)
        // 核心理念：透過 FlowLayoutPanel 的容器特性，將每個模組視為「一個按鈕 + 一個子容器」。
        // 透過遞迴深度 (Level) 動態計算左側縮排，達成視覺上的階層感。
        // =====================================================================
        private void BuildAccordionMenu()
        {
            flpMenu.Controls.Clear();
            flpMenu.SuspendLayout();

            // 預留垂直捲軸空間
            int buttonWidth = flpMenu.Width - 25;

            // 啟動遞迴：傳入 null 代表從最頂層的根模組開始，初始深度為 0
            BuildMenuLevel(parentId: null, parentContainer: flpMenu, buttonWidth: buttonWidth, level: 0);

            flpMenu.ResumeLayout();
        }

        /// <summary>
        /// 遞迴建構選單節點
        /// </summary>
        /// <param name="parentId">當下要尋找的父節點 ID</param>
        /// <param name="parentContainer">要把產生的按鈕塞進哪個容器</param>
        /// <param name="buttonWidth">按鈕統一寬度</param>
        /// <param name="level">目前所在的階層深度 (0=根模組, 1=子模組, 2=孫模組...)</param>
        private void BuildMenuLevel(int? parentId, FlowLayoutPanel parentContainer, int buttonWidth, int level)
        {
            // 撈出屬於目前 parentId 的所有節點 (排除按鈕層級 NodeType = 3)
            var nodes = _authorizedNodes
                .Where(n => n.ParentNodeID == parentId && n.NodeType != 3)
                .OrderBy(n => n.SortSeq)
                .ToList();

            foreach (var node in nodes)
            {
                // 💡 動態縮排計算：每深一層，左側向內縮進 15px (實際上不是15px)
                int leftPadding = level * 15;

                if (node.NodeType == 1) // 處理「模組」或「子模組」
                {
                    // 1. 動態建構模組按鈕
                    Button btnModule = new Button
                    {
                        Text = new string(' ', leftPadding / 3) + "📁 " + node.NodeName, // 簡單的文字縮排
                        Width = buttonWidth,
                        Height = 45,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(45, 45, 48),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("微軟正黑體", 11, FontStyle.Bold),
                        Margin = new Padding(0),
                        Cursor = Cursors.Hand
                    };
                    btnModule.FlatAppearance.BorderSize = 0;

                    // 2. 動態建構該模組專屬的子容器
                    FlowLayoutPanel pnlSubMenu = new FlowLayoutPanel
                    {
                        Width = buttonWidth,
                        AutoSize = true, // 內部有元件時自動長高
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        Margin = new Padding(0),
                        Visible = false, // 預設摺疊
                        // 隨著層級加深，背景色略微變暗以增加層次感
                        BackColor = level == 0 ? Color.FromArgb(28, 28, 28) : Color.FromArgb(20, 20, 20)
                    };

                    // 3. 綁定收合/展開事件
                    btnModule.Click += (sender, e) => pnlSubMenu.Visible = !pnlSubMenu.Visible;

                    // 4. 掛載至父容器
                    parentContainer.Controls.Add(btnModule);
                    parentContainer.Controls.Add(pnlSubMenu);

                    // 5. 🚨 核心發動：往下一層遞迴鑽入！
                    // 將剛剛建立的 pnlSubMenu 當作下一層的父容器傳遞進去
                    BuildMenuLevel(node.NodeID, pnlSubMenu, buttonWidth, level + 1);
                }
                else if (node.NodeType == 2) // 處理「作業頁面」
                {
                    Button btnPage = new Button
                    {
                        Text = new string(' ', (leftPadding + 15) / 3) + "📄 " + node.NodeName,
                        Width = buttonWidth,
                        Height = 40,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.Transparent,
                        ForeColor = Color.LightGray,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("微軟正黑體", 10, FontStyle.Regular),
                        Margin = new Padding(0),
                        Cursor = Cursors.Hand,
                        Tag = node // 綁定實體供反射提取
                    };
                    btnPage.FlatAppearance.BorderSize = 0;
                    btnPage.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 66);

                    // 綁定反射路由事件
                    btnPage.Click += PageButton_Click;

                    // 直接掛載至父容器 (不再往下遞迴)
                    parentContainer.Controls.Add(btnPage);
                }
            }
        }

        // =====================================================================
        // 🪞 [反射引擎] 頁籤生命週期路由
        // =====================================================================
        private void PageButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is SystemNode node)
            {
                string classPath = node.FormClassPath;
                if (string.IsNullOrWhiteSpace(classPath)) return;

                // 防禦：阻斷重複開頁
                foreach (TabPage tab in tabControlMain.TabPages)
                {
                    if (tab.Name == classPath)
                    {
                        tabControlMain.SelectedTab = tab;
                        return;
                    }
                }

                Type pageType = Type.GetType(classPath);
                if (pageType == null)
                {
                    MessageBox.Show($"系統找不到指定的模組實體：\n{classPath}", "載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // 實體化 BasePage 並封裝為 TabPage
                    UserControl pageInstance = (UserControl)Activator.CreateInstance(pageType);
                    pageInstance.Dock = DockStyle.Fill;

                    TabPage newTabPage = new TabPage(node.NodeName + "    ")
                    {
                        Name = classPath
                    };

                    newTabPage.Controls.Add(pageInstance);
                    tabControlMain.TabPages.Add(newTabPage);
                    tabControlMain.SelectedTab = newTabPage;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"模組載入發生異常：\n{ex.Message}", "系統錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =====================================================================
        // ❌ [自繪引擎] TabPage 關閉機制與實體記憶體釋放
        // =====================================================================
        // =====================================================================
        // ❌ [自繪引擎] 現代化頁籤狀態渲染與關閉機制
        // 核心職責：接管 GDI+ 繪圖，實作「狀態高亮」、「頂部色條」與「實體釋放」。
        // =====================================================================
        private void TabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabControl = (TabControl)sender;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            // 💡 狀態機判定：這張頁籤是不是當前被選中的那張？
            bool isSelected = (e.Index == tabControl.SelectedIndex);

            // 1. 決定視覺物理參數
            Color backColor = isSelected ? Color.White : Color.FromArgb(230, 230, 230); // 選中純白，未選中淺灰
            Color foreColor = isSelected ? Color.FromArgb(0, 122, 204) : Color.DimGray;  // 選中微軟藍，未選中深灰

            // 2. 繪製背景
            using (var bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, tabRect);
            }

            // 3. 👑 繪製頂部高亮色條 (Top Highlight Bar) - 打造現代 Web 視覺感
            if (isSelected)
            {
                using (var highlightBrush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                {
                    // 在頁籤最頂部畫一條 3 像素粗的藍色橫線
                    e.Graphics.FillRectangle(highlightBrush, tabRect.Left, tabRect.Top, tabRect.Width, 3);
                }
            }

            // 4. 繪製文字 (選中時加粗)
            int rightMargin = (e.Index == 0) ? 10 : 25;
            var textRect = new Rectangle(tabRect.Left + 5, tabRect.Top, tabRect.Width - rightMargin, tabRect.Height);
            using (var font = new Font(tabPage.Font, isSelected ? FontStyle.Bold : FontStyle.Regular))
            {
                string realText = tabPage.Text.TrimEnd();
                TextRenderer.DrawText(e.Graphics, realText, font, textRect, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // 5. 繪製關閉按鈕 (X) - 確保首頁 (Index 0) 絕對不畫
            if (e.Index > 0)
            {
                var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 7, 15, 15);

                // UX 優化：選中時 X 為明顯的黑色，未選中時為淡灰色
                Color closeColor = isSelected ? Color.Black : Color.DarkGray;
                TextRenderer.DrawText(e.Graphics, "x", new Font("Arial", 10, FontStyle.Bold), closeRect, closeColor);
            }
        }
        private void TabControlMain_MouseDown(object? sender, MouseEventArgs e)
        {
            var tabControl = (TabControl)sender;

            // 從 Index 1 開始檢查，絕對禁止關閉首頁
            for (int i = 1; i < tabControl.TabPages.Count; i++)
            {
                var tabRect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 7, 15, 15);

                if (closeRect.Contains(e.Location))
                {
                    var targetTab = tabControl.TabPages[i];
                    tabControl.TabPages.Remove(targetTab);
                    targetTab.Dispose();
                    break;
                }
            }
        }
    }
}