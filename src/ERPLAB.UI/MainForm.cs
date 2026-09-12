using ERPLAB.Models.Entities;
using ERPLAB.UI.Core;

namespace ERPLAB.UI
{
    /// <summary>
    /// 系統主框架視窗 (Main Application Frame)。
    /// 負責系統啟動後之核心 UI 佈局，包含：基於 RBAC 之動態遞迴導覽選單、
    /// 透過反射 (Reflection) 動態載入之頁籤路由，以及基於 GDI+ 之自訂頁籤渲染 (Custom Tab Drawing)。
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly List<SystemNode> _authorizedNodes;

        public MainForm()
        {
            InitializeComponent();
            _authorizedNodes = SessionContext.AuthorizedNodes ?? new List<SystemNode>();

            // 啟用 TabControl 之自訂繪製模式 (OwnerDrawFixed)，以實作自訂樣式與關閉按鈕
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
        // 預設首頁與登出機制
        // =====================================================================

        private void LoadDefaultDashboard()
        {
            // 權限動態判定：若使用者具備戰情室權限，則實例化 Dashboard 並嵌入首頁
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
                // 若無該權限，則提供基礎預設歡迎頁面
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
                this.Close(); // 關閉主框架，交由 Program.cs 接管並重啟 LoginForm
            }
        }

        // =====================================================================
        // 動態遞迴導覽選單建構 (Recursive Accordion Menu)
        // 透過 FlowLayoutPanel 之容器特性，將每個模組結構化為「觸發按鈕 + 子容器」。
        // 依據階層深度 (Level) 動態計算左側縮排，建立視覺上之樹狀層級。
        // =====================================================================
        private void BuildAccordionMenu()
        {
            flpMenu.Controls.Clear();
            flpMenu.SuspendLayout();

            // 預留垂直捲軸所需之空間
            int buttonWidth = flpMenu.Width - 25;

            // 啟動遞迴建構：自最頂層 (parentId = null) 開始，初始深度為 0
            BuildMenuLevel(parentId: null, parentContainer: flpMenu, buttonWidth: buttonWidth, level: 0);

            flpMenu.ResumeLayout();
        }

        /// <summary>
        /// 遞迴建構選單節點與容器
        /// </summary>
        /// <param name="parentId">父節點 ID</param>
        /// <param name="parentContainer">掛載按鈕之目標容器</param>
        /// <param name="buttonWidth">按鈕配置寬度</param>
        /// <param name="level">所在階層深度 (0=根模組, 1=子模組...)</param>
        private void BuildMenuLevel(int? parentId, FlowLayoutPanel parentContainer, int buttonWidth, int level)
        {
            // 查詢隸屬目前 parentId 之所有子節點 (排除功能按鈕層級 NodeType = 3)
            var nodes = _authorizedNodes
                .Where(n => n.ParentNodeID == parentId && n.NodeType != 3)
                .OrderBy(n => n.SortSeq)
                .ToList();

            foreach (var node in nodes)
            {
                // 動態縮排計算：依據深度調整左側留白空間
                int leftPadding = level * 15;

                if (node.NodeType == 1) // 處理「目錄模組」層級
                {
                    // 1. 動態建構目錄展開按鈕
                    Button btnModule = new Button
                    {
                        Text = new string(' ', leftPadding / 3) + "📁 " + node.NodeName,
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

                    // 2. 動態建構隸屬該目錄之子容器
                    FlowLayoutPanel pnlSubMenu = new FlowLayoutPanel
                    {
                        Width = buttonWidth,
                        AutoSize = true,
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        Margin = new Padding(0),
                        Visible = false, // 預設狀態為摺疊
                        // 隨層級加深微調背景色以區分視覺層次
                        BackColor = level == 0 ? Color.FromArgb(28, 28, 28) : Color.FromArgb(20, 20, 20)
                    };

                    // 3. 綁定目錄收合/展開事件
                    btnModule.Click += (sender, e) => pnlSubMenu.Visible = !pnlSubMenu.Visible;

                    // 4. 掛載至父容器
                    parentContainer.Controls.Add(btnModule);
                    parentContainer.Controls.Add(pnlSubMenu);

                    // 5. 遞迴呼叫：向下鑽取建構子層級結構
                    BuildMenuLevel(node.NodeID, pnlSubMenu, buttonWidth, level + 1);
                }
                else if (node.NodeType == 2) // 處理「作業頁面」層級
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
                        Tag = node // 綁定節點實體，供點擊時提取反射路徑
                    };
                    btnPage.FlatAppearance.BorderSize = 0;
                    btnPage.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 66);

                    // 綁定頁籤路由事件
                    btnPage.Click += PageButton_Click;

                    // 末端節點直接掛載至父容器，無需再進行遞迴
                    parentContainer.Controls.Add(btnPage);
                }
            }
        }

        // =====================================================================
        // 動態路由與頁籤實例化 (Reflection-based Routing)
        // 透過選單節點定義之類別路徑 (FormClassPath)，利用反射機制動態建立 UserControl 實體，
        // 並封裝為 TabPage 載入主畫面。包含防止重複開啟同名頁籤之處理。
        // =====================================================================
        private void PageButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is SystemNode node)
            {
                string? classPath = node.FormClassPath;
                if (string.IsNullOrWhiteSpace(classPath)) return;

                // 檢核防呆：若該頁面已開啟，則直接切換焦點，避免重複實例化
                foreach (TabPage tab in tabControlMain.TabPages)
                {
                    if (tab.Name == classPath)
                    {
                        tabControlMain.SelectedTab = tab;
                        return;
                    }
                }

                Type? pageType = Type.GetType(classPath);
                if (pageType == null)
                {
                    MessageBox.Show($"系統找不到指定的模組實體：\n{classPath}", "載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    // 實例化目標頁面並進行型別安全驗證後將其嵌合至 TabPage 中
                    if (Activator.CreateInstance(pageType) is not UserControl pageInstance)
                    {
                        MessageBox.Show($"指定的模組無法建立，或該模組並非 UI 控制項：\n{classPath}", "載入失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
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
        // 自訂頁籤渲染機制 (GDI+ Custom Tab Rendering)
        // 覆寫預設繪製邏輯，實作狀態高亮提示、色彩切換，以及動態繪製關閉按鈕 (X)。
        // =====================================================================
        private void TabControlMain_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not TabControl tabControl) return;
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            // 判斷當前繪製之頁籤是否為使用中之選取狀態
            bool isSelected = (e.Index == tabControl.SelectedIndex);

            // 1. 決定背景與文字色彩配置
            Color backColor = isSelected ? Color.White : Color.FromArgb(230, 230, 230);
            Color foreColor = isSelected ? Color.FromArgb(0, 122, 204) : Color.DimGray;

            // 2. 繪製頁籤背景
            using (var bgBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(bgBrush, tabRect);
            }

            // 3. 繪製頂部狀態高亮色條 (Top Highlight Bar)
            if (isSelected)
            {
                using (var highlightBrush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                {
                    e.Graphics.FillRectangle(highlightBrush, tabRect.Left, tabRect.Top, tabRect.Width, 3);
                }
            }

            // 4. 繪製頁籤文字 (選取時套用粗體樣式)
            int rightMargin = (e.Index == 0) ? 10 : 25;
            var textRect = new Rectangle(tabRect.Left + 5, tabRect.Top, tabRect.Width - rightMargin, tabRect.Height);
            using (var font = new Font(tabPage.Font, isSelected ? FontStyle.Bold : FontStyle.Regular))
            {
                string realText = tabPage.Text.TrimEnd();
                TextRenderer.DrawText(e.Graphics, realText, font, textRect, foreColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            // 5. 繪製關閉按鈕 (首頁頁籤 Index = 0 不繪製關閉功能)
            if (e.Index > 0)
            {
                var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 7, 15, 15);

                // 依據頁籤選取狀態調整關閉按鈕色彩，提升識別度
                Color closeColor = isSelected ? Color.Black : Color.DarkGray;
                TextRenderer.DrawText(e.Graphics, "x", new Font("Arial", 10, FontStyle.Bold), closeRect, closeColor);
            }
        }

        private void TabControlMain_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is not TabControl tabControl) return;

            // 偵測滑鼠點擊座標是否位於關閉按鈕範圍內 (避開 Index 0 之首頁頁籤)
            for (int i = 1; i < tabControl.TabPages.Count; i++)
            {
                var tabRect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + 7, 15, 15);

                if (closeRect.Contains(e.Location))
                {
                    var targetTab = tabControl.TabPages[i];
                    tabControl.TabPages.Remove(targetTab);

                    // 確實釋放資源，避免記憶體洩漏 (Memory Leak)
                    targetTab.Dispose();
                    break;
                }
            }
        }
    }
}